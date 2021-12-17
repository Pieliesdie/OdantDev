using oda;
using OdantDev.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OdantDev
{
    public class OdaViewModel : INotifyPropertyChanged
    {
        private IEnumerable<Node<StructureItem>> nodes;
        private IEnumerable<DomainDeveloper> developers;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public IEnumerable<Node<StructureItem>> Nodes { get => nodes; set { nodes = value; NotifyPropertyChanged("Nodes"); } }
        public IEnumerable<DomainDeveloper> Developers { get => developers; set { developers = value; NotifyPropertyChanged("Developers"); } }

        public Connection Connection { get; }

        public OdaViewModel(Connection connection)
        {
            this.Connection = connection;
        }
        public (bool Success, string Error) Load()
        {
            try
            {
                if (Connection.Login().Not()) { return (false, "Can't connect to oda"); }
                Connection.CoreMode = CoreMode.AddIn;
                this.Nodes = Connection.Hosts.AsParallel().Cast<Host>().Select(host => new Node<StructureItem>(host));
                this.Developers = Connection.LocalHost?.Develope?.Domains?.Cast<DomainDeveloper>();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static (bool Success, string Error) LoadOdaLibraries(DirectoryInfo OdaFolder)
        {
            try
            {
                var odaClientLibraries = new string[] { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
                var odaServerLibraries = new string[] { "odaClient.dll", "fastxmlparser.dll" };
                Extension.LoadServerLibraries(OdaFolder.FullName, Bitness.x86, odaServerLibraries);
                Extension.LoadServerLibraries(OdaFolder.FullName, Bitness.x64, odaServerLibraries);
                Extension.LoadClientLibraries(OdaFolder.FullName, odaClientLibraries);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
