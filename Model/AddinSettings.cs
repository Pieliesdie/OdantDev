using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OdantDev.Model
{
    public class AddinSettings
    {
        private string FileName = "AddinSettings.xml";
        private string templatePath = @"Templates\AddinSettings.xml";
        private XmlSerializer serializer = new XmlSerializer(typeof(AddinSettings));
        public List<string> DevExpressLibraries { get; set; }
        public List<string> OdaLibraries { get; set; }
        public List<string> LastProjectIds { get; set; }
        public bool IsSimpleTheme { get; set; }
        public bool IsAutoDetectOdaPath { get; set; }
        public string OdaFolder { get { return IsAutoDetectOdaPath ? Extension.LastOdaFolder.FullName : odaFolder; } set => odaFolder = value; }

        public AddinSettings() { }
        private string _path;
        private string odaFolder;

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
