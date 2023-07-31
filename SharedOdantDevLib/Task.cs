namespace OdantDev;

public static class TaskEx
{
    //
    // Summary:
    //     Returns a task that completes as the original task completes or when a timeout
    //     expires, whichever happens first.
    //
    // Parameters:
    //   task:
    //     The task to wait for.
    //
    //   timeout:
    //     The maximum time to wait.
    //
    // Returns:
    //     A task that completes with the result of the specified task or faults with a
    //     System.TimeoutException if timeout elapses first.
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        using CancellationTokenSource timerCancellation = new CancellationTokenSource();
        Task timeoutTask = Task.Delay(timeout, timerCancellation.Token);
        if (await Task.WhenAny(task, timeoutTask).ConfigureAwait(continueOnCapturedContext: false) == timeoutTask)
        {
            throw new TimeoutException();
        }

        timerCancellation.Cancel();
        await task.ConfigureAwait(continueOnCapturedContext: false);
    }

    //
    // Summary:
    //     Returns a task that completes as the original task completes or when a timeout
    //     expires, whichever happens first.
    //
    // Parameters:
    //   task:
    //     The task to wait for.
    //
    //   timeout:
    //     The maximum time to wait.
    //
    // Type parameters:
    //   T:
    //     The type of value returned by the original task.
    //
    // Returns:
    //     A task that completes with the result of the specified task or faults with a
    //     System.TimeoutException if timeout elapses first.
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        await ((Task)task).WithTimeout(timeout).ConfigureAwait(continueOnCapturedContext: false);
        return await task;
    }

    public static async Task StartInfiniteTask(Action action, TimeSpan delay)
    {
        while (true)
        {
            action();
            await Task.Delay(delay);
        }
    }
}