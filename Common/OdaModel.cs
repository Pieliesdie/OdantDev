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
    }
}
