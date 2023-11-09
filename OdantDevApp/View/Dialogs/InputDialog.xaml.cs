using System;
using System.Windows;

using MaterialDesignExtensions.Controls;

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
        Title = title;
        lblQuestion.Content = question;
        txtAnswer.Text = defaultAnswer;
        this.ApplyTheming();
    }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
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
}
