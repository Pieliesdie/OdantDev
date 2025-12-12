using System.Collections.Specialized;
using System.ComponentModel;

namespace OdantDev;
public class ConcatenatedCollection<TCollection, T> : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    where TCollection : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged, IList<T>
{
    private readonly TCollection firstSubCollection;
    private readonly TCollection secondSubCollection;

    public ConcatenatedCollection(TCollection first, TCollection second)
    {
        firstSubCollection = first ?? throw new ArgumentNullException(nameof(first));
        secondSubCollection = second ?? throw new ArgumentNullException(nameof(second));
        firstSubCollection.CollectionChanged += OnFirstCollectionChanged;
        secondSubCollection.CollectionChanged += OnSecondCollectionChanged;
    }

    public IEnumerator<T> GetEnumerator() => firstSubCollection.Concat(secondSubCollection).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnFirstCollectionChanged(object _, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e, 0);

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
