using System.Diagnostics;

using CommunityToolkit.Mvvm.Input;

namespace OdantDevApp.Model.ViewModels;

public partial class AddinSettings
{
    [RelayCommand]
    public void Open()
    {
        Process.Start(AddinSettingsPath);
    }
}
