using System;
using System.Windows;

using MaterialDesignExtensions.Controls;

using MaterialDesignThemes.Wpf;

namespace OdantDev.Dialogs;

/// <summary>
/// Interaction logic for InputDialog.xaml
/// </summary>s
public partial class InputDialog : Window
{
    public InputDialog(string question, string title = "Input", string defaultAnswer = "")
    {
        InitializeComponent();
        this.Title = title;
        lblQuestion.Content = question;
        txtAnswer.Text = defaultAnswer;
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
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
            ITheme theme = this.Resources.GetTheme();
            theme.SetBaseTheme(value ? Theme.Dark : Theme.Light);
            this.Resources.SetTheme(theme);
            isDarkTheme = value;
        }
    }
}
