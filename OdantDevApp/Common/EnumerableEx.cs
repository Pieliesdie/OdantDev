using System.ComponentModel;
using System.Text;

namespace OdantDevApp.Common;

internal static class EnumerableEx
{
    public static AsyncObservableCollection<T> ToAsyncObservableCollection<T>(this IEnumerable<T> list)
    {
        return new AsyncObservableCollection<T>(list);
    }
}
