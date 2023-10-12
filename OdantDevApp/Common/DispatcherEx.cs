using System;
using System.Runtime.CompilerServices;
using System.Windows;
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

    public readonly struct SwitchToUiAwaitable : INotifyCompletion
    {
        private readonly Dispatcher _dispatcher;

        public SwitchToUiAwaitable(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public SwitchToUiAwaitable GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }

        public bool IsCompleted => _dispatcher.CheckAccess();

        public void OnCompleted(Action continuation)
        {
            _dispatcher.BeginInvoke(continuation);
        }
    }
}
