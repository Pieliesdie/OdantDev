using System;
using System.Collections.Generic;
using System.Text;

namespace SharedOdantDev.Common;

public static class Disposable
{
    public static IDisposable Create(Action action)
    {
        return new AnonymousDisposable(action);
    }
    private struct AnonymousDisposable : IDisposable
    {
        private readonly Action _dispose;
        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }
        public void Dispose()
        {
            if (_dispose != null)
            {
                _dispose();
            }
        }
    }
}