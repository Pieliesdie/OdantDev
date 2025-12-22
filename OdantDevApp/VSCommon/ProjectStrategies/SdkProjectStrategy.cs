using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.Build.Construction;
using VSLangProj;
using File = System.IO.File;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public class SdkProjectStrategy(ILogger? logger) : ProjectStrategyBase(logger)
{
    private readonly ILogger? logger = logger;
    private static readonly Version DefaultVersion = new("1.0.0.0");

    private static void SetOrCreateElement(XElement parent, XName name, string value)
    {
        var el = parent.Element(name);
        if (el == null)
        {
            parent.Add(new XElement(name, value));
        }
        else el.Value = value;
    }

    private static void UpdateProjectFile(
        string path,
        Action<XNamespace, XElement> configurePropertyGroup,
        HashSet<XName>? optionalElements = null
    )
    {
        if (!File.Exists(path))
        {
            return;
        }

        var doc = XDocument.Load(path);
        if (doc.Root == null)
        {
            return;
        }

        var ns = doc.Root.Name.Namespace;
        var propertyGroup = new XElement(ns + "PropertyGroup");
        configurePropertyGroup(ns, propertyGroup);
        UpdatePropertyGroups(doc, propertyGroup, optionalElements ?? []);
        doc.Save(path);
    }

    private static void UpdatePropertyGroups(XDocument doc, XElement propertyGroup, HashSet<XName> optionalElements)
    {
        if (doc.Root == null)
        {
            return;
        }

        var ns = doc.Root.Name.Namespace;
        var target = doc.Root.Elements(ns + "PropertyGroup").ToList();

        if (target.Count == 0)
        {
            doc.Root.AddFirst(propertyGroup);
            return;
        }

        XElement? newPropGroup = null;
        foreach (var el in propertyGroup.Elements())
        {
            var existing = target.Elements(el.Name).ToList();
            if (existing.Count > 0)
            {
                foreach (var xElement in existing)
                {
                    xElement.Value = el.Value;
                }
            }
            else
            {
                if (!optionalElements.Contains(el.Name.LocalName))
                {
                    newPropGroup ??= new XElement(ns + "PropertyGroup");
                    newPropGroup.Add(new XElement(el));
                }
            }
        }

        if (newPropGroup != null)
        {
            doc.Root.AddFirst(newPropGroup);
        }
    }

    private Version GetVersionCore(Project project)
    {
        try
        {
            return base.GetVersion(project);
        }
        catch (Exception)
        {
            // ignored
        }

        var csproj = project.FullName;
        if (!File.Exists(csproj))
        {
            return DefaultVersion;
        }

        var doc = XDocument.Load(csproj);
        if (doc.Root == null)
        {
            return DefaultVersion;
        }

        var ns = doc.Root.Name.Namespace;
        var verEl = doc.Root.Descendants(ns + "Version").FirstOrDefault();

        if (verEl != null && Version.TryParse(verEl.Value.Trim(), out var v))
        {
            return v;
        }

        return DefaultVersion;
    }

    private static bool IsMatchCore(Project project)
    {
        var csproj = project.FullName;

        if (string.IsNullOrWhiteSpace(csproj) || !File.Exists(csproj))
        {
            return false;
        }

        var doc = XDocument.Load(csproj);
        if (doc.Root == null)
        {
            return false;
        }

        return doc.Root.Name == "Project" && doc.Root.Attribute("Sdk")?.Value != null;
    }

    private bool InitProjectCore(
        Project project,
        StructureItem item,
        DirectoryInfo odaFolder,
        DirectoryInfo templatesFolder
    )
    {
        project.Name = $"{item.Name}-{item.Id}";
        var csproj = project.FullName;

        var outDirParent = new FileInfo(csproj).Directory ?? throw new DirectoryNotFoundException(project.FullName);
        var propsDir = outDirParent.CreateSubdirectory("Properties");
        var launchSettingsPath = Path.Combine(propsDir.FullName, "launchSettings.json");
        var launchSettings = new JsonObject();
        if (File.Exists(launchSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(launchSettingsPath);
                launchSettings = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                //ignore
            }
        }

        var startProgram = Environment.Is64BitOperatingSystem ? "ODA.exe" : "oda.wrapper32.exe";
        var profiles = launchSettings["profiles"] = launchSettings["profiles"]?.AsObject() ?? new JsonObject();
        profiles[startProgram] = new JsonObject
        {
            ["commandName"] = "Executable",
            ["executablePath"] = Path.Combine(odaFolder.FullName, startProgram)
        };
        var tmp = launchSettingsPath + ".tmp";
        File.WriteAllText(tmp, launchSettings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(launchSettingsPath))
        {
            File.Replace(tmp, launchSettingsPath, null);
        }
        else
        {
            File.Move(tmp, launchSettingsPath);
        }

        UpdateProjectFile(csproj, (ns, pg) =>
        {
            SetOrCreateElement(pg, ns + "AssemblyName", project.Name);
            SetOrCreateElement(pg, ns + "ReferencePath", odaFolder.FullName);
            SetOrCreateElement(pg, ns + "GenerateAssemblyInfo", "false");
        });

        UpdateProjectFile($"{csproj}.user", (ns, pg) =>
            {
                SetOrCreateElement(pg, ns + "StartAction", "Program");
                SetOrCreateElement(pg, ns + "StartProgram", Path.Combine(odaFolder.FullName, startProgram));
                SetOrCreateElement(pg, ns + "StartArguments", "debug");
                SetOrCreateElement(pg, ns + "DebuggerFlavor", "ProjectDebugger");
                SetOrCreateElement(pg, ns + "ReferencePath", odaFolder.FullName);
                SetOrCreateElement(pg, ns + "ActiveDebugProfile", startProgram);
            },
            optionalElements: ["StartAction", "StartProgram", "StartArguments", "DebuggerFlavor"]
        );
        UpdateAssemblyInfoFile(project, item, templatesFolder);

        return true;
    }

    private bool UpdateAssemblyReferencesCore(
        VSProject vsProj,
        DirectoryInfo referencesFolder,
        HashSet<string> references,
        bool force
    )
    {
        var csproj = vsProj.Project.FullName;
        var project = ProjectRootElement.Open(csproj);
        if (project is null)
        {
            return false;
        }

        foreach (var reference in project.Items.Where(i => i.ItemType == "Reference").ToList())
        {
            try
            {
                UpdateAssemblyReferenceCore(reference, project, referencesFolder, references, force);
            }
            catch (Exception e)
            {
                logger?.LogCritical(e, "Error while update assembly references: {EMessage}", e.Message);
            }
        }

        project.Save();
        return true;
    }

    private bool UpdateAssemblyReferenceCore(
        ProjectItemElement reference,
        ProjectRootElement project,
        DirectoryInfo referencesFolder,
        HashSet<string> references,
        bool force
    )
    {
        var rawInclude = reference.Include;
        var assemblyName = new AssemblyName(rawInclude);
        var referenceName = $"{assemblyName.Name}.dll";

        var hintPathMetadata = reference.Metadata.FirstOrDefault(m => m.Name == "HintPath");
        var hintPath = hintPathMetadata?.Value;

        var currentPath = string.IsNullOrEmpty(hintPath)
            ? string.Empty
            : Path.GetFullPath(Path.Combine(project.DirectoryPath, hintPath));

        if (!IsReferenceOutdated(referenceName, currentPath, referencesFolder, references, force))
        {
            return true;
        }

        var newReferencePath = Path.Combine(referencesFolder.FullName, referenceName);

        if (!File.Exists(newReferencePath))
        {
            return false;
        }

        var parent = reference.Parent;
        parent.RemoveChild(reference);
        var metadata = new Dictionary<string, string>
        {
            { "HintPath", newReferencePath },
            { "Private", "False" },
            { "SpecificVersion", "False" }
        };

        project.AddItem("Reference", assemblyName.Name, metadata);

        logger?.LogInformation("'{Reference}' is updated", assemblyName.Name);
        return true;
    }

    public override bool IsMatch(Project project)
    {
        try
        {
            return IsMatchCore(project);
        }
        catch
        {
            return false;
        }
    }

    public override bool InitProject(
        Project project,
        StructureItem item,
        DirectoryInfo odaFolder,
        DirectoryInfo templatesFolder
    )
    {
        try
        {
            return InitProjectCore(project, item, odaFolder, templatesFolder);
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "SDK InitProject error: {ExMessage}", ex.Message);
            return false;
        }
    }

    public override bool UpdateReferences(
        VSProject vsProj,
        DirectoryInfo referencesFolder,
        IEnumerable<string> references,
        bool force
    )
    {
        try
        {
            return UpdateAssemblyReferencesCore(vsProj, referencesFolder, references.ToHashSet(), force);
        }
        catch (Exception e)
        {
            logger?.LogCritical(e, "Error while update assembly references : {EMessage}", e.Message);
            return false;
        }
    }

    public override Version GetVersion(Project project)
    {
        try
        {
            return GetVersionCore(project);
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "SDK GetVersion error: {ExMessage}", ex.Message);
            return DefaultVersion;
        }
    }
}