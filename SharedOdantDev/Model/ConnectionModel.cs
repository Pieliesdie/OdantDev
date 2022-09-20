using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class ConnectionModel : ObservableObject
    {
        public static readonly string[] odaClientLibraries = new string[] { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
        public static readonly string[] odaServerLibraries = new string[] { "odaClient.dll", "fastxmlparser.dll", "ucrtbase.dll" };
        private readonly ILogger logger;

        [ObservableProperty]
        private List<StructureItemViewModel<StructureItem>> hosts;

        [ObservableProperty]
        private List<DomainDeveloper> developers;

        public static List<IntPtr> ServerAssemblies { get; set; }

        public static List<Assembly> ClientAssemblies { get; set; }

        [ObservableProperty]
        private bool autoLogin;

        partial void OnAutoLoginChanged(bool value)
        {
            Connection.AutoLogin = value;
        }

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
                Connection.CoreMode = CoreMode.AddIn;
                var connected = await Task.Run(() => Connection.Login());
                if (connected.Not())
                    return (false, "Can't connect to oda");

                AutoLogin = Connection.AutoLogin;

                Hosts = await HostsListAsync();

                Developers = Connection?.LocalHost?.Develope?.Domains?.OfType<DomainDeveloper>()?.ToList();
                if (Developers.Any() && Connection?.LocalHost?.Develope is { } developDomain)
                {
                    Hosts = Hosts?.Prepend(new StructureItemViewModel<StructureItem>(developDomain, AddinSettings.IsLazyTreeLoad, logger: logger))?.ToList();
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
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

        private async Task<List<StructureItemViewModel<StructureItem>>> HostsListAsync()
        {
            return await Task.Run(async () =>
            {
                var retryCount = 10;
                while (retryCount-- > 0 && Connection?.LocalHost?.OnLine == false)
                {
                    Connection?.LocalHost?.Reset();
                    await Task.Delay(1000);
                }
                return Connection
                    ?.Hosts
                    ?.Sorted
                    ?.OfType<Host>()
                    ?.Select(host => new StructureItemViewModel<StructureItem>(host, AddinSettings.IsLazyTreeLoad, logger: logger))
                    ?.ToList();
            });
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
