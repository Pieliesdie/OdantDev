using CommunityToolkit.Mvvm.ComponentModel;
using EnvDTE80;
using MaterialDesignColors;
using MaterialDesignExtensions.Controls;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.PlatformUI;
using oda;
using OdantDev.Model;
using SharedOdantDev.Common;
using SharedOdantDev.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using File = System.IO.File;
using SharedOdanDev.OdaOverride;
using System.Diagnostics;
using MoreLinq;
using oda.OdaOverride;
using System.Windows.Input;

namespace OdantDev;

/// <summary>
/// Interaction logic for ToolWindow1Control.
/// </summary>
[ObservableObject]
public partial class ToolWindow1Control : UserControl
{
    private DirectoryInfo OdaFolder;
    private ILogger logger;
    private VisualStudioIntegration odaAddinModel;
    private bool isDarkTheme;
    private DTE2 DTE2 { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private string status;

    public bool IsBusy => !string.IsNullOrWhiteSpace(Status);

    [ObservableProperty]
    private List<RepoBaseViewModel> _groups;

    [ObservableProperty]
    private AddinSettings addinSettings;

    [ObservableProperty]
    private ConnectionModel odaModel;
    public bool IsDarkTheme
    {
        get => isDarkTheme;
        set
        {
            ITheme theme = this.Resources.GetTheme();
            theme.SetBaseTheme(value ? Theme.Dark : Theme.Light);
            this.Resources.SetTheme(theme);
            isDarkTheme = value;
        }
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
    /// </summary>
    public ToolWindow1Control(DTE2 dTE2)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DTE2 = dTE2;
        var AddinSettingsFolder = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODA", "AddinSettings"));
        AddinSettings = AddinSettings.Create(AddinSettingsFolder);
        InitializeMaterialDesign();
        InitializeComponent();
        logger = new PopupController(this.MessageContainer);
        VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        ThemeCheckBox.IsChecked = IsVisualStudioDark();
        this.DataContext = this;
    }
    private void VSColorTheme_ThemeChanged(Microsoft.VisualStudio.PlatformUI.ThemeChangedEventArgs e)
    {
        ThemeCheckBox.IsChecked = IsVisualStudioDark();
    }

    [HandleProcessCorruptedStateExceptions]
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        this.ShowException((e.ExceptionObject as Exception).Message);
    }

    private void InitializeMaterialDesign()
    {
        // Create dummy objects to force the MaterialDesign assemblies to be loaded
        // from this assembly, which causes the MaterialDesign assemblies to be searched
        // relative to this assembly's path. Otherwise, the MaterialDesign assemblies
        // are searched relative to Eclipse's path, so they're not found.
        _ = new Card();
        _ = new Hue("Dummy", Colors.Black, Colors.White);
        _ = new OpenDirectoryControl();
        _ = new MdXaml.TextToFlowDocumentConverter();
    }

    private bool InitializeOdaComponents()
    {
        OdaFolder = new DirectoryInfo(AddinSettings.SelectedOdaFolder.Path);
        var LoadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(OdaFolder);
        if (LoadOdaLibrariesResult.Success.Not())
        {
            ShowException(LoadOdaLibrariesResult.Error);
            return false;
        }
        return true;
    }

    private bool IsVisualStudioDark()
    {
        var defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
        var isDarkTheme = (384 - defaultBackground.R - defaultBackground.G - defaultBackground.B) > 0;
        return isDarkTheme;
    }

    private async void Exit(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            try
            {
                OdaModel.Dispose();
                var serverProcess = Process.GetProcesses().Where(x => x.ProcessName.Contains("odaServer")).FirstOrDefault();
                if (serverProcess is not null)
                {
                    serverProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                logger.Info(ex.Message);
            }
        });
        ShowException("Disconnected");
    }

    #region Connect to oda and get data
    private async void Connect(object sender, RoutedEventArgs e)
    {
        using var cleaner = Disposable.Create(() => Status = string.Empty);
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
            string msg = $"Can't find oda DLLs in {AddinSettings.SelectedOdaFolder}\nRun app with admin rights before start addin or repair default oda folder";
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
        Status = "Geting data from server...";
        var UpdateModelResult = await LoadModelAsync();
        if (UpdateModelResult.Success.Not())
        {
            ShowException(UpdateModelResult.Error);
            return;
        }
        odaAddinModel = new VisualStudioIntegration(AddinSettings, DTE2, logger);
        await AddinSettings.SaveAsync();
    }
    private bool CheckDllsInFolder(string folder)
    {
        return ConnectionModel.odaClientLibraries.ToList().TrueForAll(x => File.Exists(Path.Combine(folder, x)));
    }

    private async Task<(bool Success, string Error)> LoadModelAsync()
    {
        OdaModel = new ConnectionModel(new Connection(), AddinSettings, logger);
        var GetDataResult = await OdaModel.LoadAsync();
        await OdaModel.InitReposAsync();
        if (GetDataResult.Success)
        {
            spConnect.Visibility = Visibility.Collapsed;
            FoldersComboBox.IsEnabled = false;
            CommonButtons.Visibility = Visibility.Visible;
            ErrorSp.Visibility = Visibility.Collapsed;
            OdaTree.Visibility = Visibility.Visible;
            MainTabControl.Visibility = Visibility.Visible;
            //ExitButton.Visibility = Visibility.Visible;
            DeveloperCb.SelectedItem = OdaModel.Developers?.Where(x => x.FullId == AddinSettings.SelectedDevelopeDomain).FirstOrDefault();
            return (true, null);
        }
        else
        {
            return (false, GetDataResult.Error);
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
        if (OdaTree?.SelectedItem is not StructureItemViewModel<StructureItem> item)
        {
            return;
        }
        await item.RefreshAsync(true);
    }

    public void CreateDomainClick(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>)?.Item is not Domain domain)
        {
            logger?.Info("Domain can be created only from another domain");
            return;
        }
        var dialog = new Dialogs.InputDialog("Domain name", "Insert name") { IsDarkTheme = this.IsDarkTheme };
        if (!(dialog.ShowDialog() == true))
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
        var selectedItem = OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>;
        var innerItem = selectedItem?.Item;
        if (selectedItem is null || innerItem is null)
        {
            logger?.Info("Class can't be created here");
            return;
        }
        var dialog = new Dialogs.InputDialog("Class name", "Insert name") { IsDarkTheme = this.IsDarkTheme };
        if (!(dialog.ShowDialog() == true))
        {
            return;
        }
        try
        {
            innerItem?.CreateClass(dialog.Answer);

            //TODO: Подумать как обработать ситуацию с доменом модуля, тк. фактически мы не подписываемся на изменения внутреннего класса домена
            await selectedItem.RefreshAsync(true);

        }
        catch (Exception ex)
        {
            logger.Info(ex.Message);
        }
    }

    public async void RemoveItemClick(object sender, RoutedEventArgs e)
    {
        if (OdaTree?.SelectedItem is StructureItemViewModel<StructureItem> { Item: StructureItem structureItem })
        {
            try
            {
                if (Community.VisualStudio.Toolkit.VS.MessageBox.ShowConfirm($"Remove {structureItem}?"))
                {
                    structureItem.Remove();
                    //TODO: Подумать как обработать ситуацию с доменом модуля, тк. фактически мы не подписываемся на изменения внутреннего класса домена
                    await (OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>).Parent.RefreshAsync(true);
                }
            }
            catch (Exception ex) { logger.Info(ex.Message); }
            return;
        }
    }

    private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
    {
        using var statusCleaner = Disposable.Create(() => Status = string.Empty);
        Status = "Geting data from server...";
        var UpdateModelResult = await OdaModel.RefreshAsync();
        if (UpdateModelResult.Success.Not())
        {
            ShowException(UpdateModelResult.Error);
            return;
        }
    }

    private async void RefreshRepoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var statusCleaner = Disposable.Create(() => Status = string.Empty);
            Status = "Geting data from server...";
            await OdaModel.InitReposAsync();
        }
        catch (Exception ex)
        {
            ShowException(ex.Message);
        }

    }

    private void CreateRepoButton_Click(object sender, RoutedEventArgs e)
    {
        var item = new RootItem(GitClient.Client.HostUrl);
        Groups = new List<RepoBaseViewModel> { new RepoRootViewModel(item, true, false, logger) };
    }

    private async void CreateModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>)?.Item is not Class cls)
        {
            logger?.Info("Can't create module here");
            return;
        }
        try
        {
            var uploadTask = Task.Run(() =>
            {
                var moduleFolder = cls.Dir.OpenOrCreateFolder("modules");
                var templateFolder = new DirectoryInfo(Path.Combine(VsixExtension.VSIXPath.FullName, @"Templates\ProjectTemplate"));
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "AssemblyInfo.cs"), @"AssemblyInfo.cs", true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "Init.cs"), @"Init.cs", true);
                moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "TemplateProject.csproj"), @"TemplateProject.csproj", true);
                moduleFolder.ServerToFolder();
                moduleFolder.Save();
                cls.SetPrivateFieldValue("_has_module", StateBool.True);
                //cls.ReloadClassFromServer();
                //cls.RemoteClass.Rebuild();
            });
            await uploadTask;

            if (uploadTask.IsFaulted)
            {
                logger?.Info(uploadTask.Exception?.ToString());
            }
            else
            {
                logger?.Info("Module created");
                await OpenModule(cls);
            }
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }


    private void DownloadRepoButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = RepoTree?.SelectedItem as RepoBaseViewModel;
        if (selectedItem?.Item is ProjectItem project)
        {
            _ = InitRepositoryAsync(project.Object as GitLabApiClient.Models.Projects.Responses.Project);
        }
    }

    public async Task InitRepositoryAsync(GitLabApiClient.Models.Projects.Responses.Project project)
    {
        GitLabApiClient.Models.Files.Responses.File file = await GitClient.FindTopOclFileAsync(project);
        if (file == null)
        {
            logger?.Info("Classes in repository not found.");
            return;
        }

        var xmlDoc = new xmlDocument(file.ContentDecoded);
        if (xmlDoc.Root != null)
        {
            string cid = xmlDoc.Root.GetAttribute("ClassId");
            string type = xmlDoc.Root.GetAttribute("Type");

            Domain domain = OdaModel.Connection.FindDomain(OdaModel.AddinSettings.SelectedDevelopeDomain);
            if (domain != null)
            {
                var item = domain.FindItem(cid) as StructureItem;
                if (item != null)
                {
                    await Community.VisualStudio.Toolkit.VS.MessageBox.ShowAsync("Module already exists in developer domain.", "", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    logger?.Info("Module already exists in developer domain.");
                }
                else
                {
                    try
                    {
                        string rootDomainFolderPath = TempFiles.TempPath;
                        string modulePath = GitClient.CloneProject(project, rootDomainFolderPath, type == "MODULE");
                        var rootDir = new DirectoryInfo(modulePath);
                        if (!string.IsNullOrEmpty(modulePath))
                        {
                            item = OdaModel.CreateItemsFromFiles(domain, rootDir);
                        }

                        rootDir.Delete(true);
                    }
                    catch { }

                    logger?.Info("Repository has been cloned.");

                    OpenModuleDialog.DataContext = item;
                    OpenModuleDialog.IsOpen = true;
                }
            }
        }
    }

    private async void DialogOpenModule_OnClick(object sender, RoutedEventArgs e)
    {
        OpenModuleDialog.IsOpen = false;

        var item = OpenModuleDialog?.DataContext as StructureItem;
        if (item != null)
        {
            switch (item.ItemType)
            {
                case ItemType.Class:
                    {
                        await OpenModule(item);
                        break;
                    }
                case ItemType.Module:
                    {
                        foreach (Class child in item.getChilds(ItemType.Class, Deep.Near))
                        {
                            await OpenModule(child);
                        }
                        break;
                    }
            }
        }
    }

    private async void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if ((OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>)?.Item is not Class cls)
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
            await OpenModule(createdClass);
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }
    private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree.SelectedItem as StructureItemViewModel<StructureItem>;
        StructureItem structureItem = selectedItem?.Item;
        if (structureItem == null)
        {
            logger?.Info("Item not found.");
            return;
        }

        switch (structureItem.ItemType)
        {
            case ItemType.Class:
                {
                    await OpenModule(structureItem);
                    break;
                }
            case ItemType.Module:
                {
                    foreach (StructureItemViewModel<StructureItem> child in selectedItem.Children)
                    {
                        if (child.Item is Class { HasModule: true })
                            await OpenModule(child.Item);
                    }
                    break;
                }
        }
    }

    private async Task OpenModule(StructureItem item)
    {
        await odaAddinModel.OpenModuleAsync(item);
        await Task.Run(async () =>
        {
            AddinSettings.LastProjects = new ObservableCollection<AddinSettings.Project>(
                AddinSettings.LastProjects.Except(AddinSettings.LastProjects.Where(x => x.FullId == item.FullId)));
            var icon = await item.GetImageSource();
            AddinSettings.LastProjects.Add(new AddinSettings.Project(item.Name, item.FullId, item.Host.Name, DateTime.Now, icon));
            AddinSettings.LastProjects = new ObservableCollection<AddinSettings.Project>(AddinSettings.LastProjects.OrderByDescending(x => x.OpenTime).Take(15)
                ?? new List<AddinSettings.Project>());
            if (AddinSettings.Save().Not())
            {
                logger.Info("Error while saving settings");
            }
        });
    }
    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            IsDarkTheme = checkBox.IsChecked ?? false;
        }
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

        if (treeViewItem != null)
        {
            treeViewItem.Focus();
            treeViewItem.IsSelected = true;
            e.Handled = true;
        }
    }
    static TreeViewItem VisualUpwardSearch(DependencyObject source)
    {
        while (source != null && !(source is TreeViewItem))
            source = VisualTreeHelper.GetParent(source);

        return source as TreeViewItem;
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl tabControl)
        {
            if (tabControl.SelectedIndex == 0 || tabControl.SelectedIndex == 1)
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
    }

    private void RepairToolboxButton_Click(object sender, RoutedEventArgs e)
    {
        OdantDevPackage.ToolboxReseter.ResetToolboxQueue();
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
        if (sender is CheckBox checkBox)
        {
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
        if ((sender as Button)?.Tag is AddinSettings.Project project)
        {
            var isDeleted = AddinSettings.LastProjects.Remove(project);
            if (isDeleted)
            {
                if (AddinSettings.Save().Not())
                {
                    logger.Info("Error while saving settings");
                }
            }
        }
    }
    private async void ProjectCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not AddinSettings.Project project)
        {
            return;
        }
        await Task.Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(project.FullId))
            {
                logger?.Info("Can't find project's ID");
                return;
            }
            var selectedItem = await Task.Run(() => OdaModel?.Connection?.FindItem(project.FullId) as StructureItem);
            if (selectedItem == null)
            {
                logger?.Info("Can't find this project");
                return;
            }
            await OpenModule(selectedItem);
        });
    }
    private void DialogAddLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        AddinSettings.UpdateReferenceLibraries.Add(DialogAddLibrary.Text);
    }

    private void DeleteLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        AddinSettings.UpdateReferenceLibraries = new ObservableCollection<string>(AddinSettings.UpdateReferenceLibraries.Except(LibrariesList.SelectedItems.OfType<string>()));
    }

    private void DeleteOdaFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (OdaFoldersList.SelectedItems.Count == 0)
            return;
        AddinSettings.OdaFolders = new ObservableCollection<PathInfo>(AddinSettings.OdaFolders.Except(OdaFoldersList.SelectedItems.OfType<PathInfo>()));
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

    private void CreateRepo_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>;
        var selectedGroup = DialogRepoGroupTree?.SelectedItem as RepoBaseViewModel;
        string name = DialogTextBoxRepoName?.Text;
        if (selectedItem != null && selectedItem.Item != null && !string.IsNullOrWhiteSpace(name))
        {
            _ = CreateGitLabProjectAsync(selectedItem.Item, selectedGroup?.Item?.FullPath, name);
        }
    }

    private async Task<GitLabApiClient.Models.Projects.Responses.Project> CreateGitLabProjectAsync(StructureItem item, string groupPath, string name)
    {
        string modulePath = item.Dir.RemoteFolder.LoadFolder();
        modulePath = DevHelpers.ClearDomainAndClassInPath(modulePath);

        GitLabApiClient.Models.Projects.Responses.Project project = await GitClient.CreateProjectAsync(modulePath, groupPath, name);

        if (project != null)
        {
            item.Root.SetAttribute(GitClient.GIT_REPO_FIELD_NAME, project.SshUrlToRepo);
            item.Root.SetAttribute(GitClient.GIT_PROJECT_ID_FIELD_NAME, project.Id);
            item.Save();
        }
        return project;
    }

    private void DeleteRepo_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>;
        if (selectedItem == null)
            return;

        bool? isDeleteLink = DialogCheckBoxDeleteLink?.IsChecked;
        bool? isDeleteLocalRepo = DialogCheckBoxDeleteLocalRepo?.IsChecked;
        bool? isDeleteRemoteRepo = DialogCheckBoxDeleteRemoteRepo?.IsChecked;

        if (isDeleteLocalRepo.HasValue && isDeleteLocalRepo.Value)
        {
            string modulePath = selectedItem.Item.Dir.RemoteFolder.LoadFolder();
            modulePath = DevHelpers.ClearDomainAndClassInPath(modulePath);
            var directoryInfo = new DirectoryInfo(System.IO.Path.Combine(modulePath, ".git"));
            if (directoryInfo.Exists)
            {
                DevHelpers.SetAttributesNormal(directoryInfo);
                directoryInfo.Delete(true);
            }
        }

        if (isDeleteRemoteRepo.HasValue && isDeleteRemoteRepo.Value)
        {
            string projectId = selectedItem.Item.Root.GetAttribute(GitClient.GIT_PROJECT_ID_FIELD_NAME);
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                _ = GitClient.DeleteProjectAsync(projectId);
            }
        }

        if (isDeleteLink.HasValue && isDeleteLink.Value)
        {
            selectedItem.Item.Root.RemoveAttribute(GitClient.GIT_REPO_FIELD_NAME);
            selectedItem.Item.Root.RemoveAttribute(GitClient.GIT_PROJECT_ID_FIELD_NAME);
            selectedItem.Item.Save();
        }
    }
    void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }
    #endregion
}