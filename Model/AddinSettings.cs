using OdantDev.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OdantDev.Model
{
    public class AddinSettings : INotifyPropertyChanged
    {
        public struct Project
        {
            public Project(string name, string description, string fullId, string domainName, DateTime openTime)
            {
                Name = name;
                Description = description;
                FullId = fullId;
                HostName = domainName;
                OpenTime = openTime;
            }

            public string Name { get; set; }
            public string Description { get; set; }
            public string FullId { get; set; }
            public string HostName { get; set; }
            public DateTime OpenTime { get; set; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        private string FileName = "AddinSettings.xml";
        private string templatePath = Path.Combine(Extension.VSIXPath.FullName, @"Templates\AddinSettings.xml");
        private XmlSerializer serializer = new XmlSerializer(typeof(AddinSettings));
        public ObservableCollection<string> DevExpressLibraries { get => devExpressLibraries; set { devExpressLibraries = value; NotifyPropertyChanged("DevExpressLibraries"); } }
        public ObservableCollection<string> OdaLibraries { get => odaLibraries; set { odaLibraries = value; NotifyPropertyChanged("OdaLibraries"); } }
        public ObservableCollection<Project> LastProjects { get => lastProjects; set { lastProjects = value; NotifyPropertyChanged("LastProjects"); } }
        public bool IsSimpleTheme { get => isSimpleTheme; set { isSimpleTheme = value; NotifyPropertyChanged("IsSimpleTheme"); } }
        public bool IsAutoDetectOdaPath { get => isAutoDetectOdaPath; set { isAutoDetectOdaPath = value; NotifyPropertyChanged("IsAutoDetectOdaPath"); NotifyPropertyChanged("OdaFolder"); } }
        public bool IsLazyTreeLoad { get => isLazyTreeLoad; set { isLazyTreeLoad = value; NotifyPropertyChanged("IsLazyTreeLoad"); } }
        public string SelectedDevelopeDomain { get => selectedDevelopeDomain; set { selectedDevelopeDomain = value; NotifyPropertyChanged("SelectedDevelopeDomain"); } }
        public string OdaFolder
        {
            get { return IsAutoDetectOdaPath ? Extension.LastOdaFolder.FullName : odaFolder; }
            set { odaFolder = value; NotifyPropertyChanged("OdaFolder"); }
        }
        public string AddinSettingsPath => _path;
        public AddinSettings() { }
        private string _path;
        private string odaFolder;
        private bool isLazyTreeLoad;
        private bool isAutoDetectOdaPath;
        private bool isSimpleTheme;
        private ObservableCollection<Project> lastProjects;
        private ObservableCollection<string> odaLibraries;
        private ObservableCollection<string> devExpressLibraries;
        private string selectedDevelopeDomain;

        public AddinSettings(DirectoryInfo folder)
        {
            AddinSettings settings = null;
            try
            {
                var path = Path.Combine(folder.FullName, FileName);
                if (File.Exists(path).Not())
                {
                    path = templatePath;
                }
                using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                settings = (serializer.Deserialize(fs) as AddinSettings);
            }
            catch (Exception ex)
            {
                using FileStream fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
                settings = (serializer.Deserialize(fs) as AddinSettings);
            }
            DevExpressLibraries = settings.DevExpressLibraries;
            OdaLibraries = settings.OdaLibraries;
            LastProjects = new ObservableCollection<Project>(settings.LastProjects.OrderByDescending(x => x.OpenTime));
            IsAutoDetectOdaPath = settings.IsAutoDetectOdaPath;
            OdaFolder = settings.OdaFolder;
            IsSimpleTheme = settings.IsSimpleTheme;
            IsLazyTreeLoad = settings.IsLazyTreeLoad;
            SelectedDevelopeDomain = settings.SelectedDevelopeDomain;
            _path = Path.Combine(folder.FullName, FileName);
        }

        public bool Save()
        {
            try
            {
                File.Delete(_path);
                using FileStream fs = new FileStream(_path, FileMode.OpenOrCreate);
                serializer.Serialize(fs, this);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
