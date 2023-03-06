using EnvDTE;
using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Trees.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models.Users.Responses;
using Process = System.Diagnostics.Process;
using Project = GitLabApiClient.Models.Projects.Responses.Project;

namespace SharedOdantDev.Model
{
    static class GitClient
    {
        public const string GIT_REPO_FIELD_NAME = "GitLabRepository";
        public const string GIT_PROJECT_ID_FIELD_NAME = "GitLabRepositoryID";
        public static GitLabClient Client { get; set; }

        public static Session Session { get; set; }

        public static void CreateClient(string apiPath, string apiKey)
        {
            if (string.IsNullOrEmpty(apiPath) || string.IsNullOrEmpty(apiKey))
                return;
            
            Client = new GitLabClient(apiPath, apiKey);
            _ = LoadSessionAsync();
        }

        public static async Task LoadSessionAsync()
        {
            Session = await Client.Users.GetCurrentSessionAsync();
        }

        public static async Task<Project> CreateProjectAsync(string modulePath, string groupPath, string name)
        {
            string gitPath = new Uri(Client.HostUrl).Host;
            string projectPath = string.IsNullOrWhiteSpace(groupPath) ? $"{Session.Username}/{name}" : $"{groupPath}/{name}";
            var fillPath = $"git@{gitPath}:{projectPath}.git";

            RunCmdCommand($@"git init && git remote add origin {fillPath} && git add . && git commit -m ""Initial commit"" && git push -u origin master", modulePath);

            Project project = await Client.Projects.GetAsync(System.Net.WebUtility.UrlEncode(projectPath));

            return project;
        }

        public static string CloneProject(Project project, string domainFolderPath, bool isDomainModule)
        {
            try
            {
                if (!string.IsNullOrEmpty(domainFolderPath))
                {
                    //var domainFolder = new DirectoryInfo(domainFolderPath.Substring(0, domainFolderPath.Length - (domainFolderPath.Length - domainFolderPath.LastIndexOf("\\DOMAIN\\CLASS"))));
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

                    if (isSuccess)
                    {
                        DirectoryInfo moduleDir = domainFolder.CreateSubdirectory(moduleDirName);

                        RunCmdCommand($"git clone {project.SshUrlToRepo} .", moduleDir.FullName);

                        return moduleDir.FullName;
                    }
                }

                return string.Empty;
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        private static void RunCmdCommand(string command, string workingDirectory)
        {
            var procStartInfo = new ProcessStartInfo("cmd", $"/c {command}")
            {
                WorkingDirectory = workingDirectory
            };

            var proc = new Process
            {
                StartInfo = procStartInfo
            };

            proc.Start();
            proc.WaitForExit();
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
            catch (System.Exception)
            {
            }

            return null;
        }

        public static async Task DeleteProjectAsync(ProjectId projectId)
        {
            await Client.Projects.DeleteAsync(projectId);
        }
    }
}
