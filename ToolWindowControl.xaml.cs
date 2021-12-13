using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using oda;
using odaServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
namespace OdantDev
{
    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindow1Control : UserControl, INotifyPropertyChanged
    {
        private OdaModel odaModel;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public OdaModel OdaModel { get => odaModel; set { odaModel = value; NotifyPropertyChanged("OdaModel"); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            Extension.LoadClientLibraries(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignThemes.Wpf.dll", "MaterialDesignColors.dll");
            this.InitializeComponent();
        }
        #region Connect to oda and get data
        private void Connect(object sender, RoutedEventArgs e)
        {
            var LoadOdaLibrariesResult = OdaModel.LoadOdaLibraries(Extension.OdaFolder);
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
        }
        private (bool Success, string Error) LoadModel()
        {
            OdaModel = new OdaModel(Common.Connection);
            var GetDataResult = OdaModel.Load();
            if (GetDataResult.Success)
            {
                spConnect.Visibility = Visibility.Collapsed;
                CommonButtons.Visibility = Visibility.Visible;
                ErrorSp.Visibility = Visibility.Collapsed;
                OdaTree.Visibility = Visibility.Visible;
                this.DataContext = this;
                Common.Env_DTE = OdantDevPackage.Env_DTE;
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

        private void OpenModuleButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            Solution.OpenModule((OdaTree.SelectedItem as Node<StructureItem>).Item, false);

            Common.Env_DTE = OdantDevPackage.Env_DTE;

        }
        #endregion
    }
}