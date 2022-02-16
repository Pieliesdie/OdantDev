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
        public List<string> DevExpressLibraries { get; }
        public List<string> OdaLibraries { get; }
        public List<string> ServerLibraries { get; }
        public List<string> LastProjectIds { get; }

        public AddinSettings() { }
        private string _path;
        public AddinSettings(string path)
        {
            _path = path;
            DevExpressLibraries = new List<string>();
            OdaLibraries = new List<string>();
            LastProjectIds = new List<string>();
            ServerLibraries = new List<string>();
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AddinSettings));
            File.Delete(_path);
            using FileStream fs = new FileStream(_path, FileMode.OpenOrCreate);
            serializer.Serialize(fs, this);
        }
    }
}
