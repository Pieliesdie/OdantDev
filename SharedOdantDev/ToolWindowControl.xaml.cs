using EnvDTE80;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.PlatformUI;
using oda;
using OdantDev.Model;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignExtensions.Controls;
using System.Threading.Tasks;
using File = System.IO.File;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security;
using odaCore;

namespace OdantDev
{
    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindow1Control : UserControl, INotifyPropertyChanged
    {
        private bool isOdaLibraryesloaded;
        private ConnectionModel odaModel;
        private DirectoryInfo OdaFolder;
        private ILogger logger;
        private DTE2 DTE2 { get; }
        private VisualStudioIntegration odaAddinModel;
        private bool isDarkTheme;
        private AddinSettings addinSettings;
        private bool isBusy;
        private string status;

        public string Status { get => status; set { status = value; NotifyPropertyChanged("Status"); } }
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
        public bool IsBusy { get => isBusy; set { isBusy = value; NotifyPropertyChanged("IsBusy"); } }
        public event PropertyChangedEventHandler PropertyChanged;
       
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public AddinSettings AddinSettings { get => addinSettings; set { addinSettings = value; NotifyPropertyChanged("AddinSettings"); } }
        public ConnectionModel OdaModel { get => odaModel; set { odaModel = value; NotifyPropertyChanged("OdaModel"); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control(DTE2 dTE2)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DTE2 = dTE2;
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
            var card = new Card();
            var hue = new Hue("Dummy", Colors.Black, Colors.White);
            var ext = new OpenDirectoryControl();
        }

        private bool InitializeOdaComponents()
        {
            if (isOdaLibraryesloaded)
            {
                return true;
            }            

            OdaFolder = new DirectoryInfo(AddinSettings.SelectedOdaFolder.Path);
            var LoadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(OdaFolder);
            if (LoadOdaLibrariesResult.Success.Not())
            {
                ShowException(LoadOdaLibrariesResult.Error);
                return false;
            }
            return true;
        }
        private bool CheckDllsInFolder(string folder)
        {
            return ConnectionModel.odaClientLibraries.ToList().TrueForAll(x => File.Exists(Path.Combine(folder, x)));
        }
        private bool IsVisualStudioDark()
        {
            var defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var isDarkTheme = (384 - defaultBackground.R - defaultBackground.G - defaultBackground.B) > 0 ? true : false;
            return isDarkTheme;
        }

        #region Connect to oda and get data
        private async void Connect(object sender, RoutedEventArgs e)
        {
            AddinSettings.Save();

            IsBusy = true;
            Status = "Checking DLLs in oda folder";
            string odaPath = AddinSettings.SelectedOdaFolder.Path;

            if (Directory.Exists(odaPath).Not())
            {
                logger.Info($"Can't find selected oda folder {AddinSettings.SelectedOdaFolder}\nSettings was reset");
                ShowException($"Can't find selected oda folder {AddinSettings.SelectedOdaFolder}");
                IsBusy = false;
                return;
            }
              
            if (CheckDllsInFolder(odaPath).Not())
            {
                string msg = $"Can't find oda DLLs in {AddinSettings.SelectedOdaFolder}\nRun app with admin rights before start addin or repair default oda folder";
                logger.Info(msg);
                ShowException(msg);
                IsBusy = false;
                return;
            }

            Status = "Loading Oda's DLLs";
            if (isOdaLibraryesloaded = InitializeOdaComponents().Not())
            {
                ShowException($"Can't initialize oda libraries");
                IsBusy = false;
                return;
            }
            Status = "Geting data from server...";
            var UpdateModelResult = await LoadModelAsync();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                IsBusy = false;
                return;
            }
            odaAddinModel = new VisualStudioIntegration(AddinSettings, DTE2, logger);
            // Решение создатся при загрузке проекта
            //await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            //if (OdantDevPackage.Env_DTE.Solution.IsOpen.Not())
            //{
            //    OdantDevPackage.Env_DTE.Solution.Create(OdaFolder.CreateSubdirectory("AddIn").FullName, "ODANT");
            //}
            IsBusy = false;
        }
        private async Task<(bool Success, string Error)> LoadModelAsync()
        {
            OdaModel = new ConnectionModel(Common.Connection, AddinSettings, logger);
            var GetDataResult = await OdaModel.LoadAsync();
            if (GetDataResult.Success)
            {
                spConnect.Visibility = Visibility.Collapsed;
                FoldersComboBox.IsEnabled = false;
                CommonButtons.Visibility = Visibility.Visible;
                ErrorSp.Visibility = Visibility.Collapsed;
                OdaTree.Visibility = Visibility.Visible;
                MainTabControl.Visibility = Visibility.Visible;
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
            ErrorSp.Visibility = Visibility.Visible;
            spConnect.IsEnabled = true;
            FoldersComboBox.Visibility = Visibility.Visible;
            CommonButtons.Visibility = Visibility.Collapsed;
            OdaTree.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Collapsed;
            ErrorTb.Text = message;
        }
        #endregion

        #region ui button logic
        private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
        {
            IsBusy = true;
            Status = "Geting data from server...";
            var UpdateModelResult = await OdaModel.RefreshAsync();
            IsBusy = false;
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
        }
        private async void CreateModuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>)?.Item is Class cls))
            {
                try
                {
                    var uploadTask = Task.Run(() =>
                    {
                        var moduleFolder = cls.Dir.OpenOrCreateFolder("modules");
                        var templateFolder = new DirectoryInfo(Path.Combine(Extension.VSIXPath.FullName, @"Templates\ProjectTemplate"));
                        moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "AssemblyInfo.cs"), @"AssemblyInfo.cs", true);
                        moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "Init.cs"), @"Init.cs", true);
                        moduleFolder.SaveFile(Path.Combine(templateFolder.FullName, "TemplateProject.csproj"), @"TemplateProject.csproj", true);
                        moduleFolder.ServerToFolder();
                        moduleFolder.Save();
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
                        OpenModule(cls);
                    }
                }
                catch (Exception ex)
                {
                    logger?.Info(ex.ToString());
                }
            }
        }

        private void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((OdaTree?.SelectedItem as StructureItemViewModel<StructureItem>)?.Item is Class cls))
            {
                if (DeveloperCb.SelectedItem is DomainDeveloper domainDeveloper)
                {
                    try
                    {
                        var createdDomain = domainDeveloper.CreateDomain(cls.Domain.Name, "MODULE");
                        createdDomain.Save();
                        var createdClass = createdDomain.CreateClass(cls.Name);
                        createdClass.Type = cls.Type;
                        createdClass.Save();
                        cls.Dir.CopyTo(createdClass.Dir);
                        createdClass.Dir.Save();
                        createdClass.ReloadClassFromServer();
                        logger?.Info($"New module downloaded to {createdClass.FullId}");
                    }
                    catch (Exception ex)
                    {
                        logger?.Info(ex.ToString());
                    }
                }
                else
                {
                    logger?.Info("Please selecet developer domain on settings tab");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (OdaTree.SelectedItem as StructureItemViewModel<StructureItem>).Item;
            OpenModule(selectedItem);
        }
        private async void OpenModule(StructureItem item)
        {
            await odaAddinModel.OpenModuleAsync(item);
            AddinSettings.LastProjects = new ObservableCollection<AddinSettings.Project>(
                AddinSettings.LastProjects.Except(AddinSettings.LastProjects.Where(x => x.FullId == item.FullId)));
            AddinSettings.LastProjects.Add(new AddinSettings.Project(item.Name, item.Description, item.FullId, item.Host.Name, DateTime.Now));
            AddinSettings.LastProjects = new ObservableCollection<AddinSettings.Project>(AddinSettings.LastProjects.OrderByDescending(x => x.OpenTime).Take(15)
                ?? new List<AddinSettings.Project>());
            AddinSettings.Save();
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                IsDarkTheme = checkBox.IsChecked ?? false;
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedIndex == 0)
                    CommonButtons.Visibility = Visibility.Visible;
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
            try
            {
                AddinSettings.Save();
                logger.Info($"Saved in {AddinSettings.AddinSettingsPath}");
            }
            catch (Exception ex)
            {
                logger.Info(ex.Message);
            }
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
                AddinSettings.LastProjects.Remove(project);
                if (AddinSettings.Save().Not())
                {
                    logger.Info("Error while saving settings");
                }
            }
        }
        private void ProjectCard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is AddinSettings.Project project)
            {
                if (string.IsNullOrWhiteSpace(project.FullId))
                {
                    logger?.Info("Can't find project's ID");
                    return;
                }
                var selectedItem = OdaModel?.Connection?.FindItem(project.FullId) as StructureItem;
                if (selectedItem == null)
                {
                    logger?.Info("Can't find this project");
                    return;
                }
                OpenModule(selectedItem);
            }
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
        #endregion

        private void CreateItemInfo_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}