using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Requests;
using GitLabApiClient.Models.Trees.Responses;
using GitLabApiClient.Models.Users.Responses;

using oda;

using OdantDev;

using SharedOdantDev.Common;

using File = System.IO.File;
using Process = System.Diagnostics.Process;
using Project = GitLabApiClient.Models.Projects.Responses.Project;

namespace SharedOdantDev.Model;
public static class GitClientFieldName
{
    public static string GIT_REPO_HTTP => "GitLabRepositoryUrl";
    public static string GIT_REPO_SSH => "GitLabRepository";
    public static string GIT_PROJECT_ID => "GitLabRepositoryID";
}

public static class GitClient
{
    public static GitLabClient Client { get; set; }
    public static Session Session { get; set; }

    public async static Task CreateClientAsync(string apiPath, string apiKey)
    {
        if (string.IsNullOrEmpty(apiPath) || string.IsNullOrEmpty(apiKey))
            return;

        Client = new GitLabClient(apiPath, apiKey);
        await LoadSessionAsync();
    }

    public static async Task LoadSessionAsync()
    {
        Session = await Client.Users.GetCurrentSessionAsync();
    }

    public static async Task<Project> CreateProjectAsync(StructureItem item, Group group, string name)
    {
        var itemPath = item.Dir.RemoteFolder.LoadFolder();
        var moduleFolder = DevHelpers.ClearDomainAndClassInPath(itemPath);

        if (!Directory.Exists(moduleFolder))
            throw new Exception("Can't load remote folder");

        var request = CreateProjectRequest.FromName(name);
        request.NamespaceId = group?.Id;
        var project = await Client.Projects.CreateAsync(request);
        if (project != null)
        {
            item.Root.SetAttribute(GitClientFieldName.GIT_PROJECT_ID, project.Id);
            item.Root.SetAttribute(GitClientFieldName.GIT_REPO_SSH, project.SshUrlToRepo);
            item.Root.SetAttribute(GitClientFieldName.GIT_REPO_HTTP, project.HttpUrlToRepo);
            item.Save();

            var gitignore = Path.Combine(VsixExtension.VSIXPath.FullName, ".gitignore");
            File.Copy(gitignore, Path.Combine(moduleFolder, ".gitignore"));
            RunCmdCommand($"git init " +
                 $"&& git remote add origin {project.HttpUrlToRepo} " +
                 $"&& git add . " +
                 $"&& git commit -m \"Initial commit\" " +
                 $"&& git push -u origin master", moduleFolder);
        }
        return project;
    }

    public static string CloneProject(Project project, string domainFolderPath, bool isDomainModule)
    {
        try
        {
            if (string.IsNullOrEmpty(domainFolderPath))
            {
                return string.Empty;
            }

            var domainFolder = new DirectoryInfo(domainFolderPath);

            string defaultDirName = isDomainModule ? $"d.{project.Name}" : project.Name;
            string moduleDirName = defaultDirName;
            bool isSuccess = false;

            for (int i = 0; i < 100; i++)
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

            DirectoryInfo moduleDir = domainFolder.CreateSubdirectory(moduleDirName);

            RunCmdCommand($"git clone {project.SshUrlToRepo} .", moduleDir.FullName);

            return moduleDir.FullName;
        }
        catch
        {
            throw;
        }
    }

    private static void RunCmdCommand(string command, string workingDirectory)
    {
        var procStartInfo = new ProcessStartInfo("cmd", $"/c {command}")
        {
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        var proc = new Process
        {
            StartInfo = procStartInfo
        };

        proc.Start();
        proc.WaitForExit();

        var ex = proc.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(ex))
        {
            throw new Exception(ex);
        }
    }

    public static async Task<GitLabApiClient.Models.Files.Responses.File> FindTopOclFileAsync(Project project, string path = "")
    {
        if (project.DefaultBranch == null)
            return null;

        try
        {
            IList<Tree> collection = await Client.Trees.GetAsync(project.Id, x =>
            {
                x.Path = path;
            });

            foreach (Tree tree in collection)
            {
                if (tree.Name is "DOMAIN" or "CLASS")
                {
                    return await FindTopOclFileAsync(project, tree.Path);
                }

                if (tree.Name == "class.ocl")
                {
                    return await Client.Files.GetAsync(project.Id, tree.Path);
                }
            }
        }
        catch (Exception)
        {
        }

        return null;
    }

    public static async Task DeleteProjectAsync(ProjectId projectId)
    {
        await Client.Projects.DeleteAsync(projectId);
    }
}
