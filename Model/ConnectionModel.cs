using oda;
using OdantDev.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace OdantDev
{
    public class ConnectionModel : INotifyPropertyChanged
    {
        public static readonly string[] odaClientLibraries = new string[] { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
        public static readonly string[] odaServerLibraries = new string[] { "odaClient.dll", "fastxmlparser.dll", "ucrtbase.dll" };
        private readonly ILogger logger;
        private IEnumerable<StructureItemViewModel<StructureItem>> hosts;
        private IEnumerable<DomainDeveloper> developers;
        private bool autoLogin;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public IEnumerable<StructureItemViewModel<StructureItem>> Hosts { get => hosts; set { hosts = value; NotifyPropertyChanged("Hosts"); } }
        public IEnumerable<DomainDeveloper> Developers { get => developers; set { developers = value; NotifyPropertyChanged("Developers"); } }

        public static List<IntPtr> ServerAssemblies { get; set; }

        public static List<Assembly> ClientAssemblies { get; set; }
        public bool AutoLogin { get => autoLogin; set { autoLogin = value; Connection.AutoLogin = value; NotifyPropertyChanged("AutoLogin"); } }
        public Connection Connection { get; }
        public AddinSettings AddinSettings { get; }

        public ConnectionModel(Connection connection, AddinSettings addinSettings, ILogger logger = null)
        {
            this.Connection = connection;
            this.AddinSettings = addinSettings;
            this.logger = logger;
        }

        public static Bitness Platform => IntPtr.Size == 4 ? Bitness.x86 : Bitness.x64;

        [HandleProcessCorruptedStateExceptions]
        public async Task<(bool Success, string Error)> LoadAsync()
        {
            Stopwatch stopWatch = null;
            try
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                stopWatch = new Stopwatch();
                stopWatch.Start();
                Connection.CoreMode = CoreMode.Debug;
                var connected = await Task.Run(() => Connection.Login());
                if (connected.Not()) { return (false, "Can't connect to oda"); }
                this.AutoLogin = Connection.AutoLogin;
                await Task.Run(() =>
                {
                    this.Hosts = Connection.Hosts
                    .Sorted
                    .AsParallel()
                    .AsOrdered()
                    .OfType<Host>()
                    .Select(host => new StructureItemViewModel<StructureItem>(host, AddinSettings.IsLazyTreeLoad, logger: logger))
                    .ToList();
                });

                var retryCount = 5;
                while (retryCount-- > 0 && Connection.LocalHost?.Develope?.Domains == null)// Я не знаю почему оно null если обратится сразу
                {
                    await Task.Delay(1000);
                }
                this.Developers = Connection.LocalHost?.Develope?.Domains?.OfType<DomainDeveloper>();
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                logger?.Info($"Load time: {elapsedTime}");
                Common.DebugINI.Clear();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message ?? ex.ToString());
            }
            finally
            {
                stopWatch?.Stop();
            }
        }
        public async Task<(bool Success, string Error)> RefreshAsync()
        {
            try
            {
                this.Connection.ResetUser();
                this.Connection.Reset();
                return await this.LoadAsync();
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
