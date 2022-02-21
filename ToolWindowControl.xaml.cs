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
            AddinSettings = new AddinSettings(AddinSettingsFolder);
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
            OdaFolder = new DirectoryInfo(AddinSettings.OdaFolder);
            var LoadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(OdaFolder);
            if (LoadOdaLibrariesResult.Success.Not())
            {
                ShowException(LoadOdaLibrariesResult.Error);
                return false;
            }
            return true;
        }
        private bool checkDllsInFolder(string folder)
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
            if (AddinSettings.IsAutoDetectOdaPath.Not())
            {
                if (Directory.Exists(AddinSettings.OdaFolder).Not())
                {
                    logger.Info($"Can't find selected oda folder {AddinSettings.OdaFolder}\nSettings was reset");
                    AddinSettings.IsAutoDetectOdaPath = true;
                }
                else if (checkDllsInFolder(AddinSettings.OdaFolder).Not())
                {
                    logger.Info($"Can't find oda DLLs in {AddinSettings.OdaFolder}\nSettings was reset");
                    AddinSettings.IsAutoDetectOdaPath = true;
                }
            }
            if (checkDllsInFolder(AddinSettings.OdaFolder).Not())
            {
                logger.Info($"Can't find oda DLLs in {AddinSettings.OdaFolder}\nRun app with admin rights before start addin or repair default oda folder");
                return;
            }
            if (isOdaLibraryesloaded = InitializeOdaComponents().Not())
            {
                return;
            }
            IsBusy = true;
            var UpdateModelResult = await LoadModelAsync();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
            odaAddinModel = new VisualStudioIntegration(AddinSettings, DTE2, logger);
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (OdantDevPackage.Env_DTE.Solution.IsOpen.Not())
            {
                OdantDevPackage.Env_DTE.Solution.Create(OdaFolder.CreateSubdirectory("AddIn").FullName, "ODANT");
            }
            IsBusy = false;
        }
        private async Task<(bool Success, string Error)> LoadModelAsync()
        {
            IsBusy = true;
            OdaModel = new ConnectionModel(Common.Connection, AddinSettings, logger);
            var GetDataResult = await OdaModel.LoadAsync();
            if (GetDataResult.Success)
            {
                spConnect.Visibility = Visibility.Collapsed;
                CommonButtons.Visibility = Visibility.Visible;
                ErrorSp.Visibility = Visibility.Collapsed;
                OdaTree.Visibility = Visibility.Visible;
                MainTabControl.Visibility = Visibility.Visible;
                IsBusy = false;
                return (true, null);
            }
            else
            {
                IsBusy = false;
                return (false, GetDataResult.Error);
            }
        }
        private void ShowException(string message)
        {
            ErrorSp.Visibility = Visibility.Visible;
            spConnect.Visibility = Visibility.Visible;
            CommonButtons.Visibility = Visibility.Collapsed;
            OdaTree.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Collapsed;
            ErrorTb.Text = message;
        }
        #endregion

        #region main button logic
        private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var UpdateModelResult = await OdaModel.RefreshAsync();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
        }

        private async void CreateModuleButton_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Not implemented :(");
        }

        private void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Not implemented :(");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
        {
            await odaAddinModel.OpenModuleAsync((OdaTree.SelectedItem as StructureItemViewModel<StructureItem>).Item);
            AddinSettings.LastProjectIds.Add((OdaTree.SelectedItem as StructureItemViewModel<StructureItem>).Item.FullId);
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
            if(sender is CheckBox checkBox)
            {
                if (checkBox.IsChecked == true)
                {
                    var temp = TreesGrid.RowDefinitions[0].Height;
                    TreesGrid.RowDefinitions[0].Height = TreesGrid.RowDefinitions[1].Height;
                    TreesGrid.RowDefinitions[1].Height = temp;
                    SimpleOdaTree.DataContext = this;
                    OdaTree.DataContext = null;
                }
                else
                {
                    var temp = TreesGrid.RowDefinitions[1].Height;
                    TreesGrid.RowDefinitions[1].Height = TreesGrid.RowDefinitions[0].Height;
                    TreesGrid.RowDefinitions[0].Height = temp;
                    SimpleOdaTree.DataContext = null;
                    OdaTree.DataContext = this;
                }
            }
        }
        #endregion
    }
}