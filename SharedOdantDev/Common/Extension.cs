using EnvDTE80;

using Microsoft.Win32;

using oda;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OdantDev;

public static class Extension
{
    public static string SubstringBefore(this string str, string search)
    {
        var index = str.IndexOf(search);
        if (index > 0)
        {
            return str.Substring(0, index);
        }
        return str;
    }
    public static string SubstringAfter(this string str, string search, bool takeLast = false)
    {
        var index = (takeLast? str.LastIndexOf(search) : str.IndexOf(search));
        if (index >= 0)
        {
            return str.Substring(index + search.Length);
        }
        return str;
    }
    public static string Or(this string text, string alternative)
    {
        return string.IsNullOrWhiteSpace(text) ? alternative : text;
    }
    public static bool Remove<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<T> Use<T>(this IEnumerable<T> items) where T : IDisposable
    {
        foreach (var item in items)
            yield return item;
        foreach (var item in items)
        {
            if (item != null)
            {
                item.Dispose();
            }
        }
    }
    public static bool Not(this bool boolean) => !boolean;
}
