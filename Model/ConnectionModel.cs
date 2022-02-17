using oda;
using OdantDev.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OdantDev
{
    public class ConnectionModel : INotifyPropertyChanged
    {
        public static readonly string[] odaClientLibraries = new string[] { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
        public static readonly string[] odaServerLibraries = new string[] { "odaClient.dll", "fastxmlparser.dll", "ucrtbase.dll" };
        private readonly ILogger logger;
        private IEnumerable<StructureItemViewModel<StructureItem>> nodes;
        private IEnumerable<DomainDeveloper> developers;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public IEnumerable<StructureItemViewModel<StructureItem>> Nodes { get => nodes; set { nodes = value; NotifyPropertyChanged("Nodes"); } }
        public IEnumerable<DomainDeveloper> Developers { get => developers; set { developers = value; NotifyPropertyChanged("Developers"); } }

        public static List<IntPtr> ServerAssemblies { get; set; }

        public static List<Assembly> ClientAssemblies { get; set; }

        public Connection Connection { get; }

        public ConnectionModel(Connection connection, ILogger logger = null)
        {
            this.Connection = connection;
            this.logger = logger;
        }

        public static Bitness Platform => IntPtr.Size == 4 ? Bitness.x86 : Bitness.x64;
        public (bool Success, string Error) Load()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();            

                if (Connection.Login().Not()) { return (false, "Can't connect to oda"); }
                Connection.CoreMode = CoreMode.AddIn;
                this.Nodes = Connection.Hosts.OfType<Host>().AsParallel().Select(host => new StructureItemViewModel<StructureItem>(host,logger)).ToList();
                this.Developers = Connection.LocalHost?.Develope?.Domains?.Cast<DomainDeveloper>();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                logger?.Info($"Load time: {elapsedTime}");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message ?? ex.ToString());
            }
        }
        public (bool Success, string Error) Refresh()
        {
            try
            {
                this.Connection.ResetUser();
                this.Connection.Reset();
                return this.Load();
            }
            catch(Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static (bool Success, string Error) LoadOdaLibraries(DirectoryInfo OdaFolder)
        {
            try
            {
                ServerAssemblies = Extension.LoadServerLibraries(OdaFolder.FullName, Platform, odaServerLibraries);
                ClientAssemblies = Extension.LoadClientLibraries(OdaFolder.FullName, odaClientLibraries);             
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
