using oda;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev
{
    public class OdaModel : INotifyPropertyChanged
    {
        private IEnumerable<Node<StructureItem>> nodes;
        private IEnumerable<DomainDeveloper> developers;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public IEnumerable<Node<StructureItem>> Nodes { get => nodes; set { nodes = value;  NotifyPropertyChanged("Nodes");  } }

        public IEnumerable<DomainDeveloper> Developers { get => developers; set { developers = value; NotifyPropertyChanged("Developers"); } }
        public (bool Success, string Error) GetData(Connection connection)
        {
            try
            {
                if (connection.Login().Not()) { return (false, "Can't connect to oda"); }
                connection.CoreMode = CoreMode.AddIn;
                this.Nodes = connection.Hosts.Cast<Host>().AsParallel().Select(host => new Node<StructureItem>(host));
                this.Developers = connection.LocalHost?.Develope?.Domains?.Cast<DomainDeveloper>();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
