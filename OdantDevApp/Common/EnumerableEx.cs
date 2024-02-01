using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdantDevApp.Common;

internal static class EnumerableEx
{
    public static AsyncObservableCollection<T> ToAsyncObservableCollection<T>(this IEnumerable<T> list)
    {
        return new AsyncObservableCollection<T>(list);
    }
}
