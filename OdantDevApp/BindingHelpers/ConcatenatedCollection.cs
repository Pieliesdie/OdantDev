using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace OdantDev;
public class ConcatenatedCollection<Collection, T> : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    where Collection : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged, IList<T>
{
    private readonly Collection firstSubCollection;
    private readonly Collection secondSubCollection;

    public ConcatenatedCollection(Collection first, Collection second)
    {
        firstSubCollection = first ?? throw new ArgumentNullException(nameof(first));
        secondSubCollection = second ?? throw new ArgumentNullException(nameof(second));

        // Подписываемся на события CollectionChanged для входных коллекций
        firstSubCollection.CollectionChanged += OnFirstCollectionChanged;
        secondSubCollection.CollectionChanged += OnSecondCollectionChanged;
    }

    public IEnumerator<T> GetEnumerator() => firstSubCollection.Concat(secondSubCollection).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    // Обрабатываем событие CollectionChanged внутренней коллекции _first
    private void OnFirstCollectionChanged(object _, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e, 0);

    // Обрабатываем событие CollectionChanged внутренней коллекции _second
    private void OnSecondCollectionChanged(object _, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e, firstSubCollection.Count);

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e, int offset)
    {
        var args = e.Action switch
        {
            NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.NewStartingIndex + offset),
            NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(e.Action, e.OldItems, e.OldStartingIndex + offset),
            NotifyCollectionChangedAction.Move => new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.NewStartingIndex + offset, e.OldStartingIndex + offset),
            NotifyCollectionChangedAction.Replace => new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.OldItems, e.OldStartingIndex + offset),
            NotifyCollectionChangedAction.Reset => new NotifyCollectionChangedEventArgs(e.Action),
            _ => throw new ArgumentOutOfRangeException(nameof(e), $"Unsupported action: {e.Action}"),
        };
        CollectionChanged?.Invoke(this, args);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    public int Count => firstSubCollection.Count + secondSubCollection.Count;
}
