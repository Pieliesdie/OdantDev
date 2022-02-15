using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev.Model
{
    internal class AddinSettings
    {
        public ReadOnlyCollection<string> DevExpressLibraries { get; }
        public ReadOnlyCollection<string> OdaLibraries { get; }
        public ReadOnlyCollection<string> LastProjectIds { get; }

        private string path;
        public AddinSettings(string path)
        {

        }

        public void Save(string path)
        {

        }
    }
}
