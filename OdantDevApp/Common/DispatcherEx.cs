using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace OdantDevApp.Common;

public static class DispatcherEx
{
    public static SwitchToUiAwaitable SwitchToMainThread()
    {
        return SwitchToDispatcher(Application.Current.Dispatcher);
    }

    public static SwitchToUiAwaitable SwitchToDispatcher(this Dispatcher dispatcher)
    {
        return new SwitchToUiAwaitable(dispatcher);
    }

    public readonly struct SwitchToUiAwaitable(Dispatcher dispatcher) : INotifyCompletion
    {
        public SwitchToUiAwaitable GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }

        public bool IsCompleted => dispatcher.CheckAccess();

        public void OnCompleted(Action continuation)
        {
            dispatcher.BeginInvoke(continuation);
        }
    }
}
