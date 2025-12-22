namespace OdantDev;
public class Retry
{
    public static async Task<T> RetryAsync<T>(Func<T> action, Func<T, bool> successCondition, TimeSpan retryDelay, TimeSpan retryTime)
    {
        var retryAttempts = (int)(retryTime.TotalMilliseconds / retryDelay.TotalMilliseconds);
        while (retryAttempts > 1)
        {
            retryAttempts--;

            try
            {
                var result = action();
                if (successCondition(result))
                {
                    return result;
                }
            }
            catch
            {
            }
            await Task.Delay(retryDelay);
        }
        return action();
    }

    public static Task<T> RetryAsync<T>(Func<T> action, TimeSpan retryDelay, TimeSpan retryTime)
    {
        return RetryAsync(action, (_) => true, retryDelay, retryTime);
    }
}
