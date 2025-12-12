using EnvDTE;
using OdantDev.Model;
using VSLangProj;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public class SdkProjectStrategy(ILogger? logger) : IProjectStrategy
{
    public bool IsMatch(Project project)
    {
        return false;
        //try
        //{
        //    var props = project.Properties.Cast<Property>().ToList();
        //    return project.Properties.Cast<Property>().Any(p => p.Name == "Sdk");
        //}
        //catch
        //{
        //    return false;
        //}
    }

    public void InitProject(Project project, StructureItem item, DirectoryInfo odaFolder, string templatesPath)
    {
        throw new NotImplementedException();
        SetProperty(project, "AssemblyTitle", $"{item.Name}-{item.Id}");
        SetProperty(project, "Description", item.Hint ?? string.Empty);
        SetProperty(project, "Company", "Infostandart");
        SetProperty(project, "Product", item.Name);
        SetProperty(project, "Copyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");

        try
        {
            var configProps = project.ConfigurationManager.ActiveConfiguration.Properties;
            var startProgram = VsixEx.Platform == Bitness.x64 
                ? "ODA.exe"
                : "oda.wrapper32.exe";

            configProps.Item("StartAction").Value = prjStartAction.prjStartActionProgram;
            configProps.Item("StartProgram").Value = Path.Combine(odaFolder.FullName, startProgram);
            configProps.Item("StartArguments").Value = "debug";
        }
        catch (Exception ex)
        {
            logger?.Error($"SDK Init config error: {ex.Message}");
        }

        IncreaseVersion(project);
        project.Save();
    }

    public Version GetVersion(Project project)
    {
        throw new NotImplementedException();
        try
        {
            var verStr = project.Properties.Item("AssemblyVersion").Value.ToString();
            return Version.Parse(verStr);
        }
        catch
        {
            return new Version("1.0.0.0");
        }
    }

    public bool IncreaseVersion(Project project)
    {
        throw new NotImplementedException();
        try
        {
            var currentVersion = GetVersion(project);
            var currentOdantVersion = Version.Parse(Utils.Version);
            var newVer =
                $"{Utils.MajorVersion}.{Utils.ShortVersion}.{Math.Max(currentOdantVersion.Revision, currentVersion.Revision + 1)}";

            SetProperty(project, "Version", newVer);
            SetProperty(project, "AssemblyVersion", newVer);
            SetProperty(project, "FileVersion", newVer);

            project.Save();
            return true;
        }
        catch (Exception ex)
        {
            logger?.Error($"SDK IncreaseVersion error: {ex.Message}");
            return false;
        }
    }

    private void SetProperty(Project proj, string name, string value)
    {
        throw new NotImplementedException();
        try
        {
            proj.Properties.Item(name).Value = value;
        }
        catch (Exception ex)
        {
            logger?.Error($"SDK SetProperty error: {ex.Message}");
        }
    }
}