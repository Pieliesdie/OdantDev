using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace OdantDevApp.Common;

public class AsyncObservableCollection<T> : ObservableCollection<T>
{
    public AsyncObservableCollection()
    {
    }

    public AsyncObservableCollection(IEnumerable<T> list)
        : base(list)
    {
    }

    protected override async void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        await Application.Current.Dispatcher.InvokeAsync(new Action(() =>
        {
            RaiseCollectionChanged(e);
        }));
    }

    private void RaiseCollectionChanged(object param)
    {
        base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
    }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        await Dispatcher.CurrentDispatcher.InvokeAsync(new Action(() =>
        {
            RaisePropertyChanged(e);
        }));
    }

    private void RaisePropertyChanged(object param)
    {
        base.OnPropertyChanged((PropertyChangedEventArgs)param);
    }
}

