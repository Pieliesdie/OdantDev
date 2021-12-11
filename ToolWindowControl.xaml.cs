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
    public partial class ToolWindow1Control : UserControl
    {
        public OdaModel OdaModel { get; } = new OdaModel();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignThemes.Wpf.dll"));
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignColors.dll"));
            this.InitializeComponent();
            this.DataContext = this;
        }
        #region Connect to oda and get data
        private void Connect(object sender, RoutedEventArgs e)
        {
            var LoadOdaLibrariesResult = LoadOdaLibraries();
            if (LoadOdaLibrariesResult.Success.Not())
            {
                ShowException(LoadOdaLibrariesResult.Error);
                return;
            }
            var UpdateModelResult = UpdateModel(OdaModel,Common.Connection);
            if (UpdateModelResult.Success.Not())
            {
                ShowException(UpdateModelResult.Error);
                return;
            }
        }
        private (bool Success, string Error) LoadOdaLibraries()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(Extension.GetOdaPath());
                string serverCorePath = Path.Combine(fileInfo.DirectoryName, "server");
                Extension.LoadServerLibraries(serverCorePath, "x86", "odaClient.dll", "fastxmlparser.dll");
                Extension.LoadServerLibraries(serverCorePath, "x64", "odaClient.dll", "fastxmlparser.dll");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        private (bool Success, string Error) UpdateModel(OdaModel odaModel,Connection connection)
        {
            var GetDataResult = odaModel.GetData(connection);
            if (GetDataResult.Success)
            {
                spConnect.Visibility = Visibility.Collapsed;
                CommonButtons.Visibility = Visibility.Visible;
                ErrorSp.Visibility = Visibility.Collapsed;
                OdaTree.Visibility = Visibility.Visible;
                return (true,null);
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
            var UpdateModelResult = UpdateModel(OdaModel,Common.Connection);
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
            OdantDevPackage.Env_DTE.Solution.Open(@"D:\localoda\d.Develope\d.Пономарев\d.OLAP\OLAP\CLASS\modules\OLAP-1CDF86EB999F00F.sln");
            var selectedItem = (OdaTree.SelectedItem as Node<StructureItem>).Item.Dir.GetDir("modules").Path;
        }
        #endregion
    }
}