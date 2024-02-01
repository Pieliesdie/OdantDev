using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using OdantDev.Model;

namespace OdantDevApp.Common;

public class AsyncObservableCollection<T> : ObservableCollection<T>
{
    public AsyncObservableCollection() { }

    public AsyncObservableCollection(IEnumerable<T> list) : base(list) { }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() => RaiseCollectionChanged(e));
    }

    private void RaiseCollectionChanged(object param) => base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() => RaisePropertyChanged(e));
    }

    private void RaisePropertyChanged(object param) => base.OnPropertyChanged((PropertyChangedEventArgs)param);
}
