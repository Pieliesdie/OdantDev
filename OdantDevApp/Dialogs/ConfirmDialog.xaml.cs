using System.Windows;

using MaterialDesignExtensions.Controls;

using MaterialDesignThemes.Wpf;

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
        base.Title = title;
        lblQuestion.Content = question;
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        base.DialogResult = true;
    }

    bool isDarkTheme;
    public bool IsDarkTheme
    {
        get => isDarkTheme;
        set
        {
            ITheme theme = base.Resources.GetTheme();
            theme.SetBaseTheme(value ? Theme.Dark : Theme.Light);
            base.Resources.SetTheme(theme);
            isDarkTheme = value;
        }
    }
}
