
#region Namespace Imports

using System.ComponentModel;

#endregion

namespace Vasu.Wpf.Controls
{
    public class ModelBase : INotifyPropertyChanged
	{
		#region ** INotifyPropertyChanged
		public void RaisePropertyChanged(string propertyName)
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}
