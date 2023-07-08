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

    public static async Task StartInfiniteTask(Action action, TimeSpan delay)
    {
        while (true)
        {
            action();
            await Task.Delay(delay);
        }
    }
}