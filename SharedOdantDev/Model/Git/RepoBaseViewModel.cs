using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace SharedOdantDev.Model
{
    public abstract class RepoBaseViewModel : INotifyPropertyChanged
    {
        protected BaseGitItem _item;
        protected BaseGitItem _parent;

        protected int _imageIndex;
        protected bool _isExpanded;

        protected OdantDev.Model.ILogger _logger;
        protected IEnumerable<RepoBaseViewModel> _children;

        public abstract string Name { get; }

        public bool IsItemAvailable => Item != null;

        public bool HasChildren => Children.Any();

        public virtual ImageSource Icon => Item?.Icon;

        public abstract bool HasModule { get; }

        public virtual BaseGitItem Item { get => _item; protected set { _item = value; NotifyPropertyChanged("Item"); } }
        public virtual BaseGitItem Parent { get => _parent; protected set { _parent = value; NotifyPropertyChanged("Parent"); } }

        protected bool _isLazyLoading;

        protected virtual List<RepoBaseViewModel> dummyList => new() { new RepoGroupViewModel() };


        public virtual IEnumerable<RepoBaseViewModel> Children
        {
            get => _isLazyLoading ? dummyList : _children;
            set { _children = value; NotifyPropertyChanged("Children"); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    if (value && _isLazyLoading)
                    {
                        _isLazyLoading = false;
                        NotifyPropertyChanged("Children");
                    }
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
