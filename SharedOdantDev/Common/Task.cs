using Microsoft.Extensions.Logging;

using oda;

using odaServer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OdantDev;


public static class TaskEx
{
    //public static async Task OnTimeout<T>(this T t, Action<T> action, int waitms) where T : Task
    //{
    //    var delayTask = Task.Delay(waitms);
    //    if (!(await Task.WhenAny(t, delayTask) == t))
    //    {
    //        action(t);
    //        return t;
    //    }
    //    else
    //    {
    //        return delayTask;
    //    }
    //}
}