using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GitLabApiClient.Models.Groups.Responses;
using MaterialDesignColors;
using MaterialDesignExtensions.Controls;
using MaterialDesignThemes.Wpf;
using oda.OdaOverride;
using OdantDev.Dialogs;
using OdantDevApp.Common;
using OdantDevApp.Logger;
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
using ItemFactory = oda.OdaOverride.ItemFactory;
using VisualStudioIntegration = OdantDevApp.VSCommon.VisualStudioIntegration;

namespace OdantDev;

/// <summary>
/// Interaction logic for ToolWindow1Control.
/// </summary>
[ObservableObject]
public partial class ToolWindowControl
{
    private IDisposable StatusCleaner() => Disposable.Create(() => Status = string.Empty);
    private readonly ILogger<ToolWindowControl> logger;
    private VisualStudioIntegration VisualStudioIntegration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    public partial string Status { get; set; } = string.Empty;

    public bool IsBusy => !string.IsNullOrWhiteSpace(Status);
    [ObservableProperty] public partial bool IsLoadingRepos { get; set; }

    public static AddinSettings AddinSettings => AddinSettings.Instance;

    [ObservableProperty] public partial List<RepoBase>? Groups { get; set; }

    [ObservableProperty] public partial ConnectionModel OdaModel { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
    /// </summary>
    public ToolWindowControl()
    {
        InitializeMaterialDesign();
        InitializeComponent();
        this.ApplyTheming();
        logger = new SnackbarLogger<ToolWindowControl>(MessageContainer, LogLevel.Information);
        Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
    }

    private void Current_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        logger?.LogCritical(e.Exception, "Unhandeled exception: {Message}", e.Exception.Message);
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
        var loadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(odaFolder, logger);
        if (loadOdaLibrariesResult.Success)
        {
            return true;
        }
        ShowException(loadOdaLibrariesResult.Error);
        return false;
    }

    private void Exit(object sender, RoutedEventArgs e)
    {
        _ = ExitAsync();
    }

    private async Task ExitAsync()
    {
        if (!CommandLine.IsOutOfProcess)
        {
            logger.LogError("Supported only in out of process");
            return;
        }

        var confirm = ConfirmDialog.Confirm("Reboot addin?", "Confirm action");
        if (!confirm)
        {
            return;
        }

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
                logger.LogInformation(ex.Message);
            }
        });
    }

    #region Connect to oda and get data

    private void Connect(object sender, RoutedEventArgs e)
    {
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        using var cleaner = StatusCleaner();
        Status = "Checking DLLs in oda folder";
        var odaPath = AddinSettings.SelectedOdaFolder.Path;

        if (Directory.Exists(odaPath).Not())
        {
            var msg = $"Can't find selected oda folder {AddinSettings.SelectedOdaFolder}";
            logger.LogInformation(msg);
            ShowException(msg);
            return;
        }

        if (CheckDllsInFolder(odaPath).Not())
        {
            var msg = $"Can't find oda DLLs in {AddinSettings.SelectedOdaFolder}\n" +
                      $"Run app with admin rights before start addin or repair default oda folder";
            logger.LogInformation(msg);
            ShowException(msg);
            return;
        }

        Status = "Loading Oda's DLLs";
        if (InitializeOdaComponents().Not())
        {
            ShowException("Can't initialize oda libraries");
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
#if DEBUG
        var envDte = OdantDevApp.VSCommon.EnvDTE.Instance!;
#else
        var envDte = OdantDevApp.VSCommon.EnvDTE.Instance
                 ?? throw new NullReferenceException("Can't get EnvDTE2 from visual studio");
#endif
        VisualStudioIntegration = new VisualStudioIntegration(AddinSettings, envDte, logger);
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
        if (!getDataResult.Success)
        {
            return (false, getDataResult.Error);
        }

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

        DeveloperCb.SelectedItem = OdaModel
            .Developers?
            .Where(x => x.FullId == AddinSettings.SelectedDevelopeDomain)
            .FirstOrDefault()
            ?? OdaModel.Developers?.FirstOrDefault();
        return (true, null);
    }

    private async Task LoadReposAsync()
    {
        using var statusCleaner = Disposable.Create(() => IsLoadingRepos = false);
        IsLoadingRepos = true;

        var res = await OdaModel.InitReposAsync();
        if (res.Success.Not())
        {
            logger?.LogInformation($"Gitlab: {res.Error}");
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
            logger?.LogInformation("Domain can be created only from another domain");
            return;
        }

        var dialog = new Dialogs.InputDialog("Domain name", "Insert name");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            ItemFactory.CreateDomain(domain, dialog.Answer, "MODULE");
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex.Message);
        }
    }

    public void CreateClassClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree?.SelectedItem as StructureViewItem<StructureItem>;
        var innerItem = selectedItem?.Item;
        if (selectedItem is null || innerItem is null)
        {
            logger?.LogInformation("Class can't be created here");
            return;
        }

        var dialog = new Dialogs.InputDialog("Class name", "Insert name");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            innerItem?.CreateClass(dialog.Answer);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex.Message);
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
        catch (Exception ex)
        {
            logger.LogInformation(ex.Message);
        }
    }

    private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
    {
        using var statusCleaner = StatusCleaner();
        Status = "Getting data from server...";
        var updateModelResult = await OdaModel.RefreshAsync();
        if (updateModelResult.Success)
        {
            return;
        }

        ShowException(updateModelResult.Error);
    }

    private void RefreshRepoButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadReposAsync();
    }

    private void CreateRepoButton_Click(object sender, RoutedEventArgs e)
    {
        if (GitClient.Client?.HostUrl == null)
        {
            logger.LogInformation("Uri for gitlab is not specified");
            return;
        }

        var item = new RootItem(GitClient.Client.HostUrl);
        Groups = [new RepoRoot(item, false, logger)];
    }

    private void CreateModuleButton_Click(object sender, RoutedEventArgs e)
    {
        _ = CreateModuleAsync();
    }

    private async Task CreateModuleAsync()
    {
        using var statusCleaner = StatusCleaner();
        Status = "Creating module...";

        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Class cls)
        {
            logger?.LogInformation("Can't create module here");
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                var moduleFolder = cls.Dir.OpenOrCreateFolder("modules");
                var templateFolder =
                    new DirectoryInfo(Path.Combine(VsixEx.VsixPath.FullName, @"Templates\ProjectTemplate"));
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "AssemblyInfo.cs"), @"AssemblyInfo.cs",
                    true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "Init.cs"), @"Init.cs", true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "TemplateProject.csproj"),
                    @"TemplateProject.csproj", true);
                moduleFolder.ServerToFolder();
                moduleFolder.Save();
                cls.SetPrivateFieldValue("_has_module", StateBool.True);
            });

            logger?.LogInformation("Module created");
            await OpenModuleAsync(cls);
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex.ToString());
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
            logger?.LogInformation("Classes in repository not found.");
            return;
        }

        var xmlDoc = new xmlDocument(file.ContentDecoded);
        if (xmlDoc.Root == null)
        {
            return;
        }

        var cid = xmlDoc.Root.GetAttribute("ClassId");
        var type = xmlDoc.Root.GetAttribute("Type");

        var domain = CommonEx.Connection.FindDomain(OdaModel.AddinSettings.SelectedDevelopeDomain);
        if (domain == null)
        {
            return;
        }

        var item = domain.FindItem(cid) as StructureItem;
        if (item != null)
        {
            logger?.LogInformation("Module already exists in developer domain.");
            return;
        }

        try
        {
            var rootDomainFolderPath = TempFiles.TempPath;
            var modulePath = GitClient.CloneProject(project, rootDomainFolderPath, type == "MODULE");
            var rootDir = new DirectoryInfo(modulePath);
            if (!string.IsNullOrEmpty(modulePath))
            {
                item = ConnectionModel.CreateItemsFromFiles(domain, rootDir);
            }

            try
            {
                rootDir.TryDeleteDirectory();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Can't clear cache: {ExMessage}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, ex.Message);
            return;
        }

        logger?.LogInformation("Repository has been cloned.");
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

    private void CreateDeveloper(object sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.InputDialog("Developer name", "Insert name");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Domain { ItemType: ItemType.DevPart } domain)
        {
            logger?.LogInformation("Selected item is not a developer domain");
            return;
        }

        try
        {
            if (ItemFactory.CreateDomain(domain, dialog.Answer, "DEVELOPER") is not DomainDeveloper newDomain)
            {
                return;
            }
            OdaModel.Developers?.Add(newDomain);
            logger?.LogInformation("Domain created");
        }
        catch (Exception err)
        {
            logger.LogInformation(err.Message);
        }
    }

    private async void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureViewItem<StructureItem>)?.Item is not Class cls)
        {
            logger?.LogInformation("Selected item is not a class");
            return;
        }

        try
        {
            if (DeveloperCb.SelectedItem is not DomainDeveloper domainDeveloper)
            {
                logger?.LogInformation("Developer domain is not selected. Creating new one...");

                if (OdaModel.Localhost?.Develope == null)
                {
                    logger?.LogInformation("Develope part is not found on localhost.");
                    return;
                }
                var username = OdaModel.User?.Name ?? "Разработчик";
                domainDeveloper = ItemFactory.CreateDomain(OdaModel.Localhost.Develope, username, "DEVELOPER") as DomainDeveloper;
                if (domainDeveloper == null)
                {
                    logger?.LogInformation("Can't create developer domain");
                    return;
                }
                OdaModel.Developers?.Add(domainDeveloper);
                DeveloperCb.SelectedItem = domainDeveloper;
            }

            logger?.LogInformation("Start downloading module");
            var createdClass = await Task.Run(() =>
            {
                var moduleDomain = domainDeveloper.FindDomain($"D:{cls.Domain.Id}")
                                   ?? domainDeveloper.CreateDomainByXml(cls.Domain.XML);
                var createdClass = moduleDomain?.CreateClassByXml(cls.XML);

                if (createdClass == null) { return null; }

                createdClass.Type = cls.Type;
                createdClass.Save();
                cls.Dir.CopyTo(createdClass.Dir);
                createdClass.Dir.Save();
                createdClass.ReloadClassFromServer();
                createdClass.SetPrivateFieldValue("_has_module", StateBool.True);
                logger?.LogInformation("New module downloaded to {CreatedClassFullId}", createdClass.FullId);
                return createdClass;
            });
            if (createdClass is null)
            {
                logger?.LogInformation("Can't create class in developer domain");
                return;
            }

            logger?.LogInformation("Module created");
            await OpenModuleAsync(createdClass);
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex.ToString());
        }
    }

    private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree.SelectedItem as StructureViewItem<StructureItem>;
        var structureItem = selectedItem?.Item;
        if (structureItem == null || selectedItem == null)
        {
            logger?.LogInformation("Item not found.");
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
                if (selectedItem.Children != null)
                {
                    foreach (var child in selectedItem.Children)
                    {
                        if (child.Item is Class { HasModule: true })
                            await OpenModuleAsync(child.Item);
                    }
                }

                break;
            }
        }
    }

    private async Task OpenModuleAsync(StructureItem item)
    {
        var isOpened = await VisualStudioIntegration.OpenModuleAsync(item);
        if (isOpened)
        {
            logger?.LogInformation("Project loaded successfully");
        }
        var icon = await item.GetImageSourceAsync();
        AddinSettings.LastProjects.Remove(x => x.FullId == item.FullId);
        var recentProject = new RecentProject(item.Name, item.FullId, item.Host.Name, DateTime.Now, icon);
        AddinSettings.LastProjects.Insert(0, recentProject);

        _ = Task.Run(async () =>
        {
            if ((await AddinSettings.SaveAsync()).Not())
            {
                logger.LogInformation("Error while saving settings");
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

        var treeViewItem = VisualUpwardSearch(dependencyObject);

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
            logger.LogInformation("Error while saving settings");
            return;
        }

        logger.LogInformation("Saved in {AddinSettingsAddinSettingsPath}", AddinSettings.AddinSettingsPath);
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
            logger.LogInformation("Error while saving settings");
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
                logger?.LogInformation("Can't find project's ID");
                return;
            }

            var selectedItem = await Task.Run(() => CommonEx.Connection?.FindItem(project.FullId) as StructureItem);
            if (selectedItem == null)
            {
                logger?.LogInformation("Can't find this project");
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
        AddinSettings.UpdateReferenceLibraries = new AsyncObservableCollection<string>(
            AddinSettings.UpdateReferenceLibraries.Except(LibrariesList.SelectedItems.OfType<string>()));
    }

    private void DeleteOdaFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (OdaFoldersList.SelectedItems.Count == 0)
            return;
        AddinSettings.OdaFolders =
            new AsyncObservableCollection<PathInfo>(
                AddinSettings.OdaFolders.Except(OdaFoldersList.SelectedItems.OfType<PathInfo>()));
    }

    private void DialogAddOdaLibraryClick(object sender, RoutedEventArgs e)
    {
        if (CheckDllsInFolder(DialogAddOdaLibrary.Text).Not())
        {
            logger?.LogInformation("No oda libraries in {Text}", DialogAddOdaLibrary.Text);
            return;
        }

        AddinSettings.OdaFolders.Add(new PathInfo(DialogAddOdaLibraryName.Text, DialogAddOdaLibrary.Text));
    }

    private async void DownloadNet4_0_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        try
        {
            button.IsEnabled = false;
            await DevHelpers.DownloadAndCopyFramework4_0Async(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
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
            await DevHelpers.DownloadAndCopyFramework4_5Async(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
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

            var name = DialogTextBoxRepoName?.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                logger?.LogError("Name can't be empty");
                return;
            }

            var label = DialogTextBoxRepoLabel?.Text ?? name;
            var description = DialogTextBoxRepoDescription.Text;
            var selectedGroup = DialogRepoGroupTree?.SelectedItem as RepoBase;
            var group = (selectedGroup?.Item as GroupItem)?.Object as Group;
            var options = new CreateProjectOptions
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
                logger?.LogInformation("Repository was created");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, ex.Message);
        }
    }

    private void DeleteRepo_OnClick(object sender, RoutedEventArgs e)
    {
        if (OdaTree?.SelectedItem is not StructureViewItem<StructureItem> selectedItem)
            return;

        var isDeleteLink = DialogCheckBoxDeleteLink?.IsChecked;
        var isDeleteLocalRepo = DialogCheckBoxDeleteLocalRepo?.IsChecked;
        var isDeleteRemoteRepo = DialogCheckBoxDeleteRemoteRepo?.IsChecked;

        if (isDeleteLocalRepo.HasValue && isDeleteLocalRepo.Value)
        {
            var modulePath = selectedItem.Item?.Dir.RemoteFolder.LoadFolder();
            if (modulePath == null)
            {
                return;
            }
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
            var projectId = selectedItem.Item.Root.GetAttribute(GitClientFieldName.GIT_PROJECT_ID);
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