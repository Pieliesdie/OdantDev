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
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        private string FileName = "AddinSettings.xml";
        private string templatePath = @"Templates\AddinSettings.xml";
        private XmlSerializer serializer = new XmlSerializer(typeof(AddinSettings));
        public ObservableCollection<string> DevExpressLibraries { get => devExpressLibraries; set { devExpressLibraries = value; NotifyPropertyChanged("DevExpressLibraries"); } }
        public ObservableCollection<string> OdaLibraries { get => odaLibraries; set { odaLibraries = value; NotifyPropertyChanged("OdaLibraries"); } }
        public ObservableCollection<string> LastProjectIds { get => lastProjectIds; set { lastProjectIds = value; NotifyPropertyChanged("LastProjectIds"); } }
        public bool IsSimpleTheme { get => isSimpleTheme; set { isSimpleTheme = value; NotifyPropertyChanged("IsSimpleTheme"); } }
        public bool IsAutoDetectOdaPath { get => isAutoDetectOdaPath; set { isAutoDetectOdaPath = value; NotifyPropertyChanged("IsAutoDetectOdaPath"); NotifyPropertyChanged("OdaFolder"); } }
        public bool IsLazyTreeLoad { get => isLazyTreeLoad; set { isLazyTreeLoad = value; NotifyPropertyChanged("IsLazyTreeLoad"); } }
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
        private ObservableCollection<string> lastProjectIds;
        private ObservableCollection<string> odaLibraries;
        private ObservableCollection<string> devExpressLibraries;

        public AddinSettings(DirectoryInfo folder)
        {
            var path = Path.Combine(folder.FullName, FileName);
            if (File.Exists(path).Not())
            {
                path = templatePath;
            }

            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var settings = (serializer.Deserialize(fs) as AddinSettings);
            DevExpressLibraries = settings.DevExpressLibraries;
            OdaLibraries = settings.OdaLibraries;
            LastProjectIds = settings.LastProjectIds;
            IsAutoDetectOdaPath = settings.IsAutoDetectOdaPath;
            OdaFolder = settings.OdaFolder;
            IsSimpleTheme = settings.IsSimpleTheme;
            _path = Path.Combine(folder.FullName, FileName);
        }

        public void Save()
        {
            File.Delete(_path);
            using FileStream fs = new FileStream(_path, FileMode.OpenOrCreate);
            serializer.Serialize(fs, this);
        }
    }
}
