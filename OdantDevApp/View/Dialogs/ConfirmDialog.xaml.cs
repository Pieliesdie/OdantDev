using MaterialDesignExtensions.Controls;

using OdantDevApp.Common;

namespace OdantDev.Dialogs;

/// <summary>
/// Interaction logic for ConfirmDialog.xaml
/// </summary>
public partial class ConfirmDialog : MaterialWindow
{
    public static bool Confirm(string question, string title = "Input")
    {
        var dialog = new ConfirmDialog(question, title);
        dialog.ShowDialog();
        return dialog.DialogResult ?? false;
    }
    public ConfirmDialog(string question, string title = "Input")
    {
        InitializeComponent();
        Title = title;
        lblQuestion.Content = question;     
        this.ApplyTheming();
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        base.DialogResult = true;
    }
}
