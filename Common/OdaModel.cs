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
        private ObservableCollection<Node<StructureItem>> nodes;
        private ObservableCollection<DomainDeveloper> developers;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public ObservableCollection<Node<StructureItem>> Nodes { get => nodes; set { nodes = value;  NotifyPropertyChanged("Nodes");  } }

        public ObservableCollection<DomainDeveloper> Developers { get => developers; set { developers = value; NotifyPropertyChanged("Developers"); } }
        public (bool Success, string Error) GetData(Connection connection)
        {
            try
            {
                if (connection.Login().Not()) { return (false, "Can't connect to oda"); }
                connection.CoreMode = CoreMode.AddIn;
                this.Nodes = new ObservableCollection<Node<StructureItem>>(connection.Hosts.Cast<Host>().AsParallel().Select(host => Node<StructureItem>.GetChildren(host)));
                this.Developers = new ObservableCollection<DomainDeveloper>(connection.LocalHost?.Develope?.Domains?.Cast<DomainDeveloper>() ?? new List<DomainDeveloper>());
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
