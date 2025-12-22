using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignExtensions.Controls;
using MaterialDesignThemes.Wpf;
using OdantDevApp.Common;

namespace OdantDevApp.Dialogs;

public enum MessageDialogIcon
{
    Default = 0,
    Information,
    Warning,
    Error
}

/// <summary>
/// Interaction logic for MessageDialog.xaml
/// </summary>
[ObservableObject]
public partial class MessageDialog : MaterialWindow
{
    private readonly Action<string>? copyAction;

    public static bool Show(
        string question,
        string title = "Information",
        MessageDialogIcon icon = MessageDialogIcon.Default,
        Action<string>? copyAction = null
    )
    {
        return new MessageDialog(question, title, icon, copyAction).ShowDialog() ?? false;
    }

    public MessageDialog()
    {
        InitializeComponent();
        this.ApplyTheming();
    }

    public MessageDialog(
        string question,
        string title = "Information",
        MessageDialogIcon icon = MessageDialogIcon.Default,
        Action<string>? copyAction = null
    ) : this()
    {
        this.copyAction = copyAction;
        Title = title;
        TextContent = question;
        TextIcon = icon switch
        {
            MessageDialogIcon.Information => PackIconKind.InformationCircleOutline,
            MessageDialogIcon.Warning => PackIconKind.WarningCircleOutline,
            MessageDialogIcon.Error => PackIconKind.Error,
            _ => PackIconKind.InformationCircleOutline
        };
        TextIconBrush = icon switch
        {
            MessageDialogIcon.Information => Brushes.DarkSeaGreen,
            MessageDialogIcon.Warning => Brushes.LightGoldenrodYellow,
            MessageDialogIcon.Error => Brushes.IndianRed,
            _ => Brushes.DarkSeaGreen
        };
    }

    [ObservableProperty] public partial PackIconKind? TextIcon { get; set; }

    [ObservableProperty] public partial Brush? TextIconBrush { get; set; }

    [ObservableProperty] public partial string? TextContent { get; set; }

    private void btnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var content = TextContent ?? string.Empty;
        if (copyAction != null)
        {
            copyAction?.Invoke(content);
        }
        else
        {
            Clipboard.SetText(content);
        }
    }
}