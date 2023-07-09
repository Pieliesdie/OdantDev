using System;
using System.Windows;

using MaterialDesignExtensions.Controls;

using MaterialDesignThemes.Wpf;

using OdantDevApp.Common;

namespace OdantDev.Dialogs;

/// <summary>
/// Interaction logic for InputDialog.xaml
/// </summary>s
public partial class InputDialog : MaterialWindow
{
    public InputDialog(string question, string title = "Input", string defaultAnswer = "")
    {
        InitializeComponent();
        base.Title = title;
        lblQuestion.Content = question;
        txtAnswer.Text = defaultAnswer;
        IsDarkTheme = AppSettings.DarkTheme;
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        base.DialogResult = true;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        txtAnswer.SelectAll();
        txtAnswer.Focus();
    }

    public string Answer
    {
        get { return txtAnswer.Text; }
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
