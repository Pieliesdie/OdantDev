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

        private void Connect(object sender, RoutedEventArgs e)
        {
            FileInfo fileInfo = new FileInfo(Extension.GetOdaPath());
            string serverCorePath = Path.Combine(fileInfo.DirectoryName, "server");
            Extension.LoadServerLibraries(serverCorePath, "x86", "odaClient.dll", "fastxmlparser.dll");
            if (!Common.Connection.Login()) return;
            Common.Connection.CoreMode = CoreMode.AddIn;
            OdaModel.Nodes = new ObservableCollection<Node<StructureItem>>(Common.Connection.Hosts.Cast<Host>().AsParallel().Select(host => host.GetChildren()));
            //OdaTree.ItemsSource = Common.Connection.Hosts.Cast<Host>().AsParallel().Select(host => host.GetChildren());
            spConnect.Visibility = Visibility.Collapsed;
            OdaModel.Developers = new ObservableCollection<DomainDeveloper>(Common.Connection.LocalHost.Develope.Domains.Cast<DomainDeveloper>());
            CommonButtons.Visibility = Visibility.Visible;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
           // OdaModel.Nodes = null;
            OdaModel.Nodes = new ObservableCollection<Node<StructureItem>>(Common.Connection.Hosts.Cast<Host>().AsParallel().Select(host => host.GetChildren())); ;
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button3_Click_1(object sender, RoutedEventArgs e)
        {
            var root = Common.Connection.LocalHost.FindDomain("H:1D670A1783307C2/D:WORK/D:1D71734AE4F4847");

        }
    }
}