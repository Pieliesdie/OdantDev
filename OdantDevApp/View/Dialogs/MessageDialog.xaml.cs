using System.Windows;
using MaterialDesignExtensions.Controls;
using OdantDevApp.Common;

namespace OdantDevApp.Dialogs;

/// <summary>
/// Interaction logic for MessageDialog.xaml
/// </summary>
public partial class MessageDialog : MaterialWindow
{
    public static bool Show(string question, string title = "Information")
    {
        return new MessageDialog(question, title).ShowDialog() ?? false;
    }

    public MessageDialog()
    {
        InitializeComponent();
        this.ApplyTheming();
    }

    public MessageDialog(string question, string title = "Information") : this()
    {
        Title = title;
        lblQuestion.Content = question;
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}