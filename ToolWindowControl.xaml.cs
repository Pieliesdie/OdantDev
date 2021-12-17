﻿using EnvDTE80;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using oda;
using OdantDev.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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
        private OdaViewModel odaModel;
        private DTE2 DTE2 { get; }

        private OdaAddinModel odaAddinModel;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public OdaViewModel OdaModel { get => odaModel; set { odaModel = value; NotifyPropertyChanged("OdaModel"); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control(DTE2 dTE2)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            InitializeMaterialDesign();
            this.InitializeComponent();
            this.DTE2 = dTE2;
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

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

        #region Connect to oda and get data
        private void Connect(object sender, RoutedEventArgs e)
        {
            var LoadOdaLibrariesResult = OdaViewModel.LoadOdaLibraries(Extension.OdaFolder);
            if (LoadOdaLibrariesResult.Success.Not())
            {
                ShowException(LoadOdaLibrariesResult.Error);
                return;
            }
            var UpdateModelResult = LoadModel();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
            odaAddinModel = new OdaAddinModel(Extension.OdaFolder, DTE2);
        }
        private (bool Success, string Error) LoadModel()
        {
            OdaModel = new OdaViewModel(Common.Connection);
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
            var UpdateModelResult = LoadModel();
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
        }

        private void CreateModuleButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DownloadModuleButton_Click(object sender, RoutedEventArgs e)
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        private async void OpenModuleButton_Click(object sender, RoutedEventArgs e)
        {
            await odaAddinModel.OpenModuleAsync((OdaTree.SelectedItem as Node<StructureItem>).Item);
            //EnvDTE.Solution.AddFromFile
        }
        #endregion
    }
}