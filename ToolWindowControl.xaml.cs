using EnvDTE80;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.PlatformUI;
using oda;
using OdantDev.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OdantDev
{
    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindow1Control : UserControl, INotifyPropertyChanged
    {
        private ConnectionModel odaModel;
        private DirectoryInfo OdaFolder;
        private ILogger logger;
        private DTE2 DTE2 { get; }
        private VisualStudioIntegration odaAddinModel;
        private bool isDarkTheme;
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public ConnectionModel OdaModel { get => odaModel; set { odaModel = value; NotifyPropertyChanged("OdaModel"); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control(DTE2 dTE2)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DTE2 = dTE2;
            InitializeMaterialDesign();
            InitializeComponent();
            InitializeOdaComponents();
            logger = new PopupController(this.MessageContainer);
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
            ThemeCheckBox.IsChecked = IsVisualStudioDark();
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
        }

        private void InitializeOdaComponents()
        {
            OdaFolder = Extension.OdaFolder;
            var LoadOdaLibrariesResult = ConnectionModel.LoadOdaLibraries(OdaFolder);
            if (LoadOdaLibrariesResult.Success.Not())
            {
                ShowException(LoadOdaLibrariesResult.Error);
                return;
            }
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
            var UpdateModelResult = LoadModel();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
            odaAddinModel = new VisualStudioIntegration(OdaFolder, DTE2);
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (OdantDevPackage.Env_DTE.Solution.IsOpen.Not())
            {
                OdantDevPackage.Env_DTE.Solution.Create(OdaFolder.CreateSubdirectory("AddIn").FullName, "ODANT");
            }
        }
        private (bool Success, string Error) LoadModel()
        {
            OdaModel = new ConnectionModel(Common.Connection,logger);
            var GetDataResult = OdaModel.Load();
            if (GetDataResult.Success)
            {
                spConnect.Visibility = Visibility.Collapsed;
                CommonButtons.Visibility = Visibility.Visible;
                ErrorSp.Visibility = Visibility.Collapsed;
                OdaTree.Visibility = Visibility.Visible;
                this.DataContext = this;
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
            spConnect.Visibility = Visibility.Visible;
            CommonButtons.Visibility = Visibility.Collapsed;
            OdaTree.Visibility = Visibility.Collapsed;
            ErrorTb.Text = message;
        }
        #endregion

        #region main button logic
        private void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var UpdateModelResult = OdaModel.Refresh();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
        }

        private void CreateModuleButton_Click(object sender, RoutedEventArgs e)
        {
            MessageContainer.MessageQueue.Enqueue("test!");
            settings.Save();

        }

        private void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
        {
            await odaAddinModel.OpenModuleAsync((OdaTree.SelectedItem as StructureItemViewModel<StructureItem>).Item);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                IsDarkTheme = checkBox.IsChecked ?? false;
            }
        }
        #endregion
    }
}