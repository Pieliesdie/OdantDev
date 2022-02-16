using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev.Model
{
    internal class PopupController : ILogger
    {
        private readonly Snackbar snackbar;
        public void Info(string message)
        {
            snackbar?.MessageQueue.Enqueue(message);
        }
        public PopupController(Snackbar snackbar)
        {
            this.snackbar = snackbar;
        }
    }
}
