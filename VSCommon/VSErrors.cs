using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace OdantDev
{
    public static class VSErrors
    {
        private static ErrorListProvider ErrorListProvider { get; set; }
        public static async void ShowError(this Project context, string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ErrorListProvider = ErrorListProvider ?? new ErrorListProvider(new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)context.DTE));
            var ivsSolution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
            ivsSolution.GetProjectOfUniqueName(context.FileName, out var hierarchyItem);
            var newError = new ErrorTask()
            {
                ErrorCategory = TaskErrorCategory.Error,
                SubcategoryIndex = 0,
                Category = TaskCategory.BuildCompile,
                Text = text,
                HierarchyItem = hierarchyItem
            };
            ErrorListProvider.Tasks.Clear();
            ErrorListProvider.Tasks.Add(newError);  // add item
            ErrorListProvider.Show();
        }
    }
}
