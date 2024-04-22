using System;
using System.IO;
using System.Threading.Tasks;

using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Requests;
using GitLabApiClient.Models.Users.Responses;

using oda;

using OdantDev;

using SharedOdantDev.Common;

using File = System.IO.File;
using Project = GitLabApiClient.Models.Projects.Responses.Project;

namespace OdantDevApp.Model.Git;
public static class GitClientFieldName
{
    public const string GIT_REPO_HTTP = "GitLabRepositoryUrl";
    public const string GIT_REPO_SSH = "GitLabRepository";
    public const string GIT_PROJECT_ID = "GitLabRepositoryID";
}

public class CreateProjectOptions
{
    public required StructureItem Item { get; init; }
    public Group? Group { get; init; }
    public required string Name { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public static class GitClient
{
    private static string PrivateKey { get; set; }
    public static GitLabClient? Client { get; set; }
    public static Session? Session { get; set; }

    public static async Task CreateClientAsync(string apiPath, string apiKey)
    {
        if (string.IsNullOrEmpty(apiPath) || string.IsNullOrEmpty(apiKey))
            return;

        PrivateKey = apiKey;
        Client = new GitLabClient(apiPath, apiKey);
        await LoadSessionAsync(Client);
    }

    public static async Task LoadSessionAsync(GitLabClient client)
    {
        Session = await client.Users.GetCurrentSessionAsync();
    }

    public static async Task<Project?> CreateProjectAsync(CreateProjectOptions options)
    {
        if (Client == null)
            return null;

        var item = options.Item;
        var itemPath = item.Dir.RemoteFolder.LoadFolder();
        var moduleFolder = DevHelpers.ClearDomainAndClassInPath(itemPath);

        if (!Directory.Exists(moduleFolder))
            throw new Exception("Can't load remote folder");

        var request = CreateProjectRequest.FromPath(options.Name);
        request.NamespaceId = options.Group?.Id;
        request.Description = options.Description;
        var project = await Client.Projects.CreateAsync(request) ?? throw new Exception("Unknown error from gitlab");

        project = await Client.Projects.UpdateAsync(project.Id, new UpdateProjectRequest(options.Label));

        item.Root.SetAttribute(GitClientFieldName.GIT_PROJECT_ID, project.Id);
        item.Root.SetAttribute(GitClientFieldName.GIT_REPO_SSH, project.SshUrlToRepo);
        item.Root.SetAttribute(GitClientFieldName.GIT_REPO_HTTP, project.HttpUrlToRepo);
        item.Save();

        var sourceGitignorePath = Path.Combine(VsixExtension.VSIXPath.FullName, ".gitignore");
        var destinationGitignorePath = Path.Combine(moduleFolder, ".gitignore");
        if (!File.Exists(destinationGitignorePath))
        {
            File.Copy(sourceGitignorePath, destinationGitignorePath);
        }
        await DevHelpers.InvokeCmdCommandAsync(
            $"git config core.safecrlf false" +
            $"&& git init" +
            $"&& git remote add origin {project.HttpUrlToRepo} " +
            $"&& git add . " +
            $"&& git commit -m \"Initial commit\" " +
            $"&& git push -u origin master", moduleFolder);
        return project;
    }

    public static string CloneProject(Project project, string domainFolderPath, bool isDomainModule)
    {
        if (string.IsNullOrEmpty(domainFolderPath))
        {
            return string.Empty;
        }

        var domainFolder = new DirectoryInfo(domainFolderPath);

        var defaultDirName = isDomainModule ? $"d.{project.Name}" : project.Name;
        var moduleDirName = defaultDirName;
        var isSuccess = false;

        for (var i = 0; i < 100; i++)
        {
            if (!Directory.Exists(Path.Combine(domainFolder.FullName, moduleDirName)))
            {
                isSuccess = true;
                break;
            }

            moduleDirName = $"{defaultDirName}_{i}";
        }

        if (!isSuccess)
        {
            return string.Empty;
        }

        var moduleDir = domainFolder.CreateSubdirectory(moduleDirName);

        var repoUri = new Uri(project.HttpUrlToRepo);
        var cloneUri = $"{repoUri.Scheme}://oauth2:{PrivateKey}@{repoUri.Host}{repoUri.AbsolutePath}";

        DevHelpers.InvokeCmdCommand($"git clone \"{cloneUri}\" . --quiet", moduleDir.FullName);

        return moduleDir.FullName;
    }

    public static async Task<GitLabApiClient.Models.Files.Responses.File?> FindTopOclFileAsync(Project project, string path = "")
    {
        if (project.DefaultBranch == null || Client == null)
            return null;

        try
        {
            var collection = await Client.Trees.GetAsync(project.Id, x =>
            {
                x.Path = path;
            });

            foreach (var tree in collection)
            {
                switch (tree.Name)
                {
                    case "DOMAIN" or "CLASS":
                        return await FindTopOclFileAsync(project, tree.Path);
                    case "class.ocl":
                        return await Client.Files.GetAsync(project.Id, tree.Path);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public static async Task DeleteProjectAsync(ProjectId projectId)
    {
        if (Client == null) return;
        await Client.Projects.DeleteAsync(projectId);
    }
}
