using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MaterialDesignExtensions.Controls;

using MaterialDesignThemes.Wpf;

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
    }
    public MessageDialog(string question, string title = "Information")
    {
        InitializeComponent();
        base.Title = title;
        lblQuestion.Content = question;
        IsDarkTheme = AppSettings.DarkTheme;
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
