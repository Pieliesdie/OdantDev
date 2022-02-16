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
        public static string FileName = "AddinSettings.xml";
        private XmlSerializer serializer = new XmlSerializer(typeof(AddinSettings));
        public ReadOnlyCollection<string> DevExpressLibraries { get; }
        public ReadOnlyCollection<string> OdaLibraries { get; }
        public ReadOnlyCollection<string> ServerLibraries { get; }
        public List<string> LastProjectIds { get; }

        public AddinSettings() { }
        private string _path;
        public AddinSettings(string path)
        {
            _path = path;
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var settings = (serializer.Deserialize(fs) as AddinSettings);
            DevExpressLibraries = settings.DevExpressLibraries;
            OdaLibraries = settings.OdaLibraries;
            ServerLibraries = settings.ServerLibraries;
            LastProjectIds = settings.LastProjectIds;
        }

        public void Save()
        {
            File.Delete(_path);
            using FileStream fs = new FileStream(_path, FileMode.OpenOrCreate);
            serializer.Serialize(fs, this);
        }
    }
}
