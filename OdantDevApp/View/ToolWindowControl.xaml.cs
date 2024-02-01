using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using GitLabApiClient.Models.Groups.Responses;

using MaterialDesignColors;

using MaterialDesignExtensions.Controls;

using MaterialDesignThemes.Wpf;

using oda;
using oda.OdaOverride;

using OdantDev.Dialogs;
using OdantDev.Model;

using OdantDevApp.Common;
using OdantDevApp.Model;
using OdantDevApp.Model.Git;
using OdantDevApp.Model.Git.GitItems;
using OdantDevApp.Model.ViewModels;
using OdantDevApp.Model.ViewModels.Settings;

using SharedOdanDev.OdaOverride;

using SharedOdantDev.Common;

using File = System.IO.File;
using GroupItem = OdantDevApp.Model.Git.GitItems.GroupItem;
using RepoBase = OdantDevApp.Model.Git.RepoBase;

namespace OdantDev;

/// <summary>
/// Interaction logic for ToolWindow1Control.
/// </summary>
[ObservableObject]
public partial class ToolWindowControl : UserControl
{
    private IDisposable StatusCleaner() => Disposable.Create(() => Status = string.Empty);
    private readonly ILogger logger;
    private VisualStudioIntegration? visualStudioIntegration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private string status = string.Empty;
    public bool IsBusy => !string.IsNullOrWhiteSpace(Status);
    [ObservableProperty] bool isLoadingRepos;
    public static AddinSettings AddinSettings => AddinSettings.Instance;

    [ObservableProperty] private List<RepoBase>? groups;

    [ObservableProperty] private ConnectionModel? odaModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
    /// </summary>
    public ToolWindowControl()
    {
        InitializeMaterialDesign();
        InitializeComponent();
        this.ApplyTheming();
        logger = new PopupController(MessageContainer);
        Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
    }
    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        logger?.Exception(e.Exception);
    }

    private static void InitializeMaterialDesign()
    {
        // Create dummy objects to force the MaterialDesign assemblies to be loaded
        // from this assembly, which causes the MaterialDesign assemblies to be searched
        // relative to this assembly's path. Otherwise, the MaterialDesign assemblies
        // are searched relative to Eclipse's path, so they're not found.
        //_ = new MaterialWindow();
        _ = new Card();
        _ = new Hue("Dummy", Colors.Black, Colors.White);
        _ = new OpenDirectoryControl();
        _ = new MdXaml.TextToFlowDocumentConverter();
    }
    private bool InitializeOdaComponents()
    {
        var odaFolder = new DirectoryInfo(AddinSettings.SelectedOdaFolder.Path);
        var loadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(odaFolder);
        if (loadOdaLibrariesResult.Success)
            return true;
        ShowException(loadOdaLibrariesResult.Error);
        return false;
    }
    private async void Exit(object sender, RoutedEventArgs e)
    {
        if (!CommandLine.IsOutOfProcess)
        {
            logger.Error("Supported only in out of process");
            return;
        }
        var confirm = ConfirmDialog.Confirm("Reboot addin?", "Confirm action");
        if (!confirm) { return; }

        var closeServer = ConfirmDialog.Confirm("Reboot server?", "Confirm action");
        var odaNames = new[] { "odaServer", "oda.events" };

        using var clean = StatusCleaner();
        Status = "Exiting...";

        await Task.Run(() =>
        {
            try
            {
                if (closeServer)
                {
                    Status = "Waiting oda processes to shutdown...";
                    foreach (var process in Process.GetProcesses())
                    {
                        var isOdaProcess = odaNames.Any(process.ProcessName.Contains);
                        if (isOdaProcess)
                        {
                            process.TrySoftKill();
                        }
                    }
                }
                Environment.Exit((int)ExitCodes.Restart);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
                logger.Info(ex.Message);
            }
        });
    }

    #region Connect to oda and get data
    private async void Connect(object sender, RoutedEventArgs e)
    {
        using var cleaner = StatusCleaner();
        Status = "Checking DLLs in oda folder";
        string odaPath = AddinSettings.SelectedOdaFolder.Path;

        if (Directory.Exists(odaPath).Not())
        {
            string msg = $"Can't find selected oda folder {AddinSettings.SelectedOdaFolder}";
            logger.Info(msg);
            ShowException(msg);
            return;
        }

        if (CheckDllsInFolder(odaPath).Not())
        {
            string msg = $"Can't find oda DLLs in {AddinSettings.SelectedOdaFolder}\n" +
                $"Run app with admin rights before start addin or repair default oda folder";
            logger.Info(msg);
            ShowException(msg);
            return;
        }

        Status = "Loading Oda's DLLs";
        if (InitializeOdaComponents().Not())
        {
            ShowException($"Can't initialize oda libraries");
            return;
        }
        Status = "Getting data from server...";
        var updateModelResult = await LoadModelAsync();
        if (updateModelResult.Success.Not())
        {
            ShowException(updateModelResult.Error);
            return;
        }
        await DispatcherEx.SwitchToMainThread();
        visualStudioIntegration = new VisualStudioIntegration(AddinSettings, OdantDevApp.VSCommon.EnvDTE.Instance, logger);
        await AddinSettings.SaveAsync();
    }
    private static bool CheckDllsInFolder(string folder)
    {
        return ConnectionModel.OdaClientLibraries.ToList().TrueForAll(x => File.Exists(Path.Combine(folder, x)));
    }
    private async Task<(bool Success, string Error)> LoadModelAsync()
    {
        Utils.MainSynchronizationContext = SynchronizationContext.Current;
        var connection = CommonEx.Connection = new Connection();
        OdaModel = new ConnectionModel(connection, AddinSettings, logger);
        var getDataResult = await OdaModel.LoadAsync();
        _ = LoadReposAsync();
        if (getDataResult.Success)
        {
            spConnect.Visibility = Visibility.Collapsed;
            FoldersComboBox.IsEnabled = false;
            CommonButtons.Visibility = Visibility.Visible;
            ErrorSp.Visibility = Visibility.Collapsed;
            OdaTree.Visibility = Visibility.Visible;
            MainTabControl.Visibility = Visibility.Visible;
            if (CommandLine.IsOutOfProcess)
            {
                ExitButton.Visibility = Visibility.Visible;
            }
            DeveloperCb.SelectedItem = OdaModel.Developers?.Where(x => x.FullId == AddinSettings.SelectedDevelopeDomain).FirstOrDefault();
            return (true, null);
        }
        else
        {
            return (false, getDataResult.Error);
        }
    }

    private async Task LoadReposAsync()
    {
        using var statusCleaner = Disposable.Create(() => IsLoadingRepos = false);
        IsLoadingRepos = true;
        var res = await OdaModel.InitReposAsync();
        if (res.Success.Not())
        {
            logger?.Info($"Gitlab: {res.Error}");
        }
    }
    private void ShowException(string message)
    {
        spConnect.Visibility = Visibility.Visible;
        FoldersComboBox.IsEnabled = true;
        CommonButtons.Visibility = Visibility.Collapsed;
        ErrorSp.Visibility = Visibility.Visible;
        OdaTree.Visibility = Visibility.Collapsed;
        MainTabControl.Visibility = Visibility.Collapsed;
        ExitButton.Visibility = Visibility.Collapsed;

        spConnect.IsEnabled = true;
        ErrorTb.Text = message;
    }
    #endregion

    #region ui button logic
    public async void RefreshItem(object sender, RoutedEventArgs e)
    {
        if (OdaTree?.SelectedItem is not StructureViewItem<StructureItem> item)
        {
            return;
        }
        await item.RefreshAsync(true);
    }
    public void CreateDomainClick(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Domain domain)
        {
            logger?.Info("Domain can be created only from another domain");
            return;
        }
        var dialog = new Dialogs.InputDialog("Domain name", "Insert name");
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        try
        {
            var newDomain = oda.OdaOverride.ItemFactory.CreateDomain(domain, dialog.Answer, "MODULE");
        }
        catch (Exception ex)
        {
            logger.Info(ex.Message);
        }
    }
    public async void CreateClassClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree?.SelectedItem as StructureViewItem<StructureItem>;
        var innerItem = selectedItem?.Item;
        if (selectedItem is null || innerItem is null)
        {
            logger?.Info("Class can't be created here");
            return;
        }
        var dialog = new Dialogs.InputDialog("Class name", "Insert name");
        if (dialog.ShowDialog() != true) { return; }
        try
        {
            innerItem?.CreateClass(dialog.Answer);
        }
        catch (Exception ex)
        {
            logger.Info(ex.Message);
        }
    }
    public void RemoveItemClick(object sender, RoutedEventArgs e)
    {
        if (OdaTree?.SelectedItem is not StructureViewItem<StructureItem> { Item: { } structureItem })
            return;

        try
        {
            var dialog = new ConfirmDialog($"Remove {structureItem}?", "");
            if (dialog.ShowDialog() != true)
                return;

            structureItem.Remove();
        }
        catch (Exception ex) { logger.Info(ex.Message); }
    }
    private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
    {
        using var statusCleaner = StatusCleaner();
        Status = "Getting data from server...";
        var updateModelResult = await OdaModel.RefreshAsync();
        if (updateModelResult.Success)
            return;

        ShowException(updateModelResult.Error);
    }
    private void RefreshRepoButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadReposAsync();

    }
    private void CreateRepoButton_Click(object sender, RoutedEventArgs e)
    {
        var item = new RootItem(GitClient.Client.HostUrl);
        Groups = new List<RepoBase> { new RepoRoot(item, false, logger) };
    }
    private async void CreateModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Class cls)
        {
            logger?.Info("Can't create module here");
            return;
        }
        try
        {
            await Task.Run(() =>
            {
                var moduleFolder = cls.Dir.OpenOrCreateFolder("modules");
                var templateFolder = new DirectoryInfo(Path.Combine(VsixExtension.VSIXPath.FullName, @"Templates\ProjectTemplate"));
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "AssemblyInfo.cs"), @"AssemblyInfo.cs", true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "Init.cs"), @"Init.cs", true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "TemplateProject.csproj"), @"TemplateProject.csproj", true);
                moduleFolder.ServerToFolder();
                moduleFolder.Save();
                cls.SetPrivateFieldValue("_has_module", StateBool.True);
            });

            logger?.Info("Module created");
            await OpenModuleAsync(cls);
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }
    private void DownloadRepoButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = RepoTree?.SelectedItem as RepoBase;
        if (selectedItem?.Item is ProjectItem project)
        {
            _ = InitRepositoryAsync(project.Object as GitLabApiClient.Models.Projects.Responses.Project);
        }
    }
    public async Task InitRepositoryAsync(GitLabApiClient.Models.Projects.Responses.Project project)
    {
        var file = await GitClient.FindTopOclFileAsync(project);
        if (file == null)
        {
            logger?.Info("Classes in repository not found.");
            return;
        }

        var xmlDoc = new xmlDocument(file.ContentDecoded);
        if (xmlDoc.Root == null)
        {
            return;
        }
        string cid = xmlDoc.Root.GetAttribute("ClassId");
        string type = xmlDoc.Root.GetAttribute("Type");

        Domain domain = CommonEx.Connection.FindDomain(OdaModel.AddinSettings.SelectedDevelopeDomain);
        if (domain == null)
        {
            return;
        }
        var item = domain.FindItem(cid) as StructureItem;
        if (item != null)
        {
            logger?.Info("Module already exists in developer domain.");
            return;
        }
        try
        {
            string rootDomainFolderPath = TempFiles.TempPath;
            string modulePath = GitClient.CloneProject(project, rootDomainFolderPath, type == "MODULE");
            var rootDir = new DirectoryInfo(modulePath);
            if (!string.IsNullOrEmpty(modulePath))
            {
                item = ConnectionModel.CreateItemsFromFiles(domain, rootDir);
            }

            rootDir.Delete(true);
        }
        catch (Exception ex)
        {
            logger?.Error(ex.Message);
        }

        logger?.Info("Repository has been cloned.");

        OpenModuleDialog.DataContext = item;
        OpenModuleDialog.IsOpen = true;
    }
    private async void DialogOpenModule_OnClick(object sender, RoutedEventArgs e)
    {
        OpenModuleDialog.IsOpen = false;

        if (OpenModuleDialog?.DataContext is not StructureItem item)
        {
            return;
        }
        switch (item.ItemType)
        {
            case ItemType.Class:
                {
                    await OpenModuleAsync(item);
                    break;
                }
            case ItemType.Module:
                {
                    foreach (Class child in item.getChilds(ItemType.Class, Deep.Near))
                    {
                        await OpenModuleAsync(child);
                    }
                    break;
                }
        }
    }
    private async void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Class cls)
        {
            logger?.Info("Selected item is not a class");
            return;
        }
        if (DeveloperCb.SelectedItem is not DomainDeveloper domainDeveloper)
        {
            logger?.Info("Please select developer domain on settings tab");
            return;
        }

        try
        {
            logger?.Info("Start downloading module");
            var createdClass = await Task.Run(() =>
            {
                Domain createdDomain = domainDeveloper.CreateDomain(cls.Domain.Name, "MODULE");
                createdDomain.Save();
                Class createdClass = createdDomain.CreateClass(cls.Name);
                createdClass.Type = cls.Type;
                createdClass.Save();
                cls.Dir.CopyTo(createdClass.Dir);
                createdClass.Dir.Save();
                createdClass.ReloadClassFromServer();
                createdClass.SetPrivateFieldValue("_has_module", StateBool.True);
                logger?.Info($"New module downloaded to {createdClass.FullId}");
                return createdClass;
            });
            if (createdClass is null)
            {
                logger?.Info("Can't create class in developer domain");
                return;
            }
            logger?.Info("Module created");
            await OpenModuleAsync(createdClass);
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }
    private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree.SelectedItem as StructureViewItem<StructureItem>;
        StructureItem structureItem = selectedItem?.Item;
        if (structureItem == null || selectedItem == null)
        {
            logger?.Info("Item not found.");
            return;
        }

        switch (structureItem.ItemType)
        {
            case ItemType.Class:
                {
                    await OpenModuleAsync(structureItem);
                    break;
                }
            case ItemType.Module:
                {
                    foreach (StructureViewItem<StructureItem> child in selectedItem.Children)
                    {
                        if (child.Item is Class { HasModule: true })
                            await OpenModuleAsync(child.Item);
                    }
                    break;
                }
        }
    }
    private async Task OpenModuleAsync(StructureItem item)
    {
        await visualStudioIntegration.OpenModuleAsync(item);

        var icon = await item.GetImageSourceAsync();
        AddinSettings.LastProjects.Remove(x => x.FullId == item.FullId);
        AddinSettings.LastProjects.Insert(0, new RecentProject(item.Name, item.FullId, item.Host.Name, DateTime.Now, icon));

        _ = Task.Run(async () =>
        {
            if ((await AddinSettings.SaveAsync()).Not())
            {
                logger.Info("Error while saving settings");
            }
        });
    }

    private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject dependencyObject)
            return;

        TreeViewItem treeViewItem = VisualUpwardSearch(dependencyObject);

        if (treeViewItem == null)
            return;

        treeViewItem.Focus();
        treeViewItem.IsSelected = true;
        e.Handled = true;
    }

    private static TreeViewItem? VisualUpwardSearch(DependencyObject source)
    {
        while (source != null && source is not TreeViewItem)
            source = VisualTreeHelper.GetParent(source);

        return source as TreeViewItem;
    }
    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl tabControl)
            return;

        if (tabControl.SelectedIndex is 0 or 1)
        {
            CommonButtons.Visibility = Visibility.Visible;

            if (tabControl.SelectedIndex == 0)
            {
                CreateModuleButton.Visibility = Visibility.Visible;
                OpenModuleButton.Visibility = Visibility.Visible;
            }
            else
            {
                CreateModuleButton.Visibility = Visibility.Hidden;
                OpenModuleButton.Visibility = Visibility.Hidden;
            }
        }
        else
            CommonButtons.Visibility = Visibility.Collapsed;
    }
    private void RepairToolboxButton_Click(object sender, RoutedEventArgs e)
    {
        //OdantDevPackage.ToolboxReseter.ResetToolboxQueue();
    }
    private void RawEditConfigButton_Click(object sender, RoutedEventArgs e)
    {
        //Process.Start(AddinSettings.AddinSettingsPath);
    }
    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (AddinSettings.Save().Not())
        {
            logger.Info("Error while saving settings");
            return;
        }
        logger.Info($"Saved in {AddinSettings.AddinSettingsPath}");
    }
    private void IsSimpleThemeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
            return;

        if (checkBox.IsChecked == true)
        {
            var style = TryFindResource("simpleTree");
            OdaTree.ItemContainerStyle = style as Style;
        }
        else
        {
            var style = TryFindResource("defaultTree");
            OdaTree.ItemContainerStyle = style as Style;
        }
    }
    private void DeveloperCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            AddinSettings.SelectedDevelopeDomain = (comboBox.SelectedValue as DomainDeveloper)?.FullId;
        }
    }
    private void DeleteRecentlyProject_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not RecentProject project)
            return;
        var isDeleted = AddinSettings.LastProjects?.Remove(project) ?? false;
        if (!isDeleted)
            return;
        if (AddinSettings.Save().Not())
        {
            logger.Info("Error while saving settings");
        }
    }
    private async void ProjectCard_Click(object sender, RoutedEventArgs e)
    {
        using var clean = StatusCleaner();
        Status = "Opening project";
        if ((sender as Button)?.Tag is not RecentProject project)
        {
            return;
        }
        CardClickDialogHost.IsOpen = false;
        CardClickDialogHost.UpdateLayout();
        await Task.Delay(1000);
        await Task.Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(project.FullId))
            {
                logger?.Info("Can't find project's ID");
                return;
            }
            var selectedItem = await Task.Run(() => CommonEx.Connection?.FindItem(project.FullId) as StructureItem);
            if (selectedItem == null)
            {
                logger?.Info("Can't find this project");
                return;
            }
            await OpenModuleAsync(selectedItem);
        });
    }
    private void DialogAddLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        AddinSettings.UpdateReferenceLibraries.Add(DialogAddLibrary.Text);
    }
    private void DeleteLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        AddinSettings.UpdateReferenceLibraries = new AsyncObservableCollection<string>(AddinSettings.UpdateReferenceLibraries.Except(LibrariesList.SelectedItems.OfType<string>()));
    }
    private void DeleteOdaFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (OdaFoldersList.SelectedItems.Count == 0)
            return;
        AddinSettings.OdaFolders = new AsyncObservableCollection<PathInfo>(AddinSettings.OdaFolders.Except(OdaFoldersList.SelectedItems.OfType<PathInfo>()));
    }
    private void DialogAddOdaLibraryClick(object sender, RoutedEventArgs e)
    {
        if (CheckDllsInFolder(DialogAddOdaLibrary.Text).Not())
        {
            logger?.Info($"No oda libraries in {DialogAddOdaLibrary.Text}");
            return;
        }
        AddinSettings.OdaFolders.Add(new PathInfo(DialogAddOdaLibraryName.Text, DialogAddOdaLibrary.Text));
    }
    private async void DownloadNet4_0_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;
        try
        {
            button.IsEnabled = false;
            await DevHelpers.DownloadAndCopyFramework4_0Async(this.logger);
        }
        catch (Exception ex)
        {
            logger.Info(ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
    private async void DownloadNet4_5_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;
        try
        {
            button.IsEnabled = false;
            await DevHelpers.DownloadAndCopyFramework4_5Async(this.logger);
        }
        catch (Exception ex)
        {
            logger.Info(ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
    #endregion

    #region gitlab
    private async void CreateRepo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (OdaTree?.SelectedItem is not StructureViewItem<StructureItem> selectedItem || selectedItem.Item == null)
            {
                return;
            }
            string name = DialogTextBoxRepoName?.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                logger?.Error("Name can't be empty");
                return;
            }
            string label = DialogTextBoxRepoLabel?.Text ?? name;
            string description = DialogTextBoxRepoDescription.Text;
            var selectedGroup = DialogRepoGroupTree?.SelectedItem as RepoBase;
            var group = (selectedGroup?.Item as GroupItem)?.Object as Group;
            var options = new CreateProjectOptions()
            {
                Name = name,
                Item = selectedItem.Item,
                Group = group,
                Label = label,
                Description = description
            };
            var createdProject = await GitClient.CreateProjectAsync(options);
            if (createdProject != null)
            {
                logger?.Info("Repository was created");
            }
        }
        catch (Exception ex)
        {
            logger?.Error(ex.Message);
        }
    }

    private void DeleteRepo_OnClick(object sender, RoutedEventArgs e)
    {
        if (OdaTree?.SelectedItem is not StructureViewItem<StructureItem> selectedItem)
            return;

        bool? isDeleteLink = DialogCheckBoxDeleteLink?.IsChecked;
        bool? isDeleteLocalRepo = DialogCheckBoxDeleteLocalRepo?.IsChecked;
        bool? isDeleteRemoteRepo = DialogCheckBoxDeleteRemoteRepo?.IsChecked;

        if (isDeleteLocalRepo.HasValue && isDeleteLocalRepo.Value)
        {
            string modulePath = selectedItem.Item.Dir.RemoteFolder.LoadFolder();
            modulePath = DevHelpers.ClearDomainAndClassInPath(modulePath);
            var directoryInfo = new DirectoryInfo(Path.Combine(modulePath, ".git"));
            if (directoryInfo.Exists)
            {
                DevHelpers.SetAttributesNormal(directoryInfo);
                directoryInfo.Delete(true);
            }
        }

        if (isDeleteRemoteRepo.HasValue && isDeleteRemoteRepo.Value)
        {
            string projectId = selectedItem.Item.Root.GetAttribute(GitClientFieldName.GIT_PROJECT_ID);
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                _ = GitClient.DeleteProjectAsync(projectId);
            }
        }

        if (isDeleteLink.HasValue && isDeleteLink.Value)
        {
            selectedItem.Item.Root.RemoveAttribute(GitClientFieldName.GIT_REPO_SSH);
            selectedItem.Item.Root.RemoveAttribute(GitClientFieldName.GIT_PROJECT_ID);
            selectedItem.Item.Save();
        }
    }
    #endregion
}