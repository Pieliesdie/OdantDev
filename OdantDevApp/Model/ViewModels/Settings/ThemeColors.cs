using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using MaterialDesignColors;

using MaterialDesignThemes.Wpf;

using OdantDev;

namespace OdantDevApp.Model.ViewModels.Settings;

[ObservableObject]
public partial class ThemeColors : ITheme
{
    public static ThemeColors Default
    {
        get
        {
            var bundledTheme = new BundledTheme
            {
                BaseTheme = BaseTheme.Light,
                PrimaryColor = PrimaryColor.DeepPurple,
                SecondaryColor = SecondaryColor.Purple,
                ColorAdjustment = new()
            };
            var theme = bundledTheme.GetTheme();
            return theme.Map<ThemeColors>();
        }
    }

    [ObservableProperty] ColorPair primaryLight;
    [ObservableProperty] ColorPair primaryMid;
    [ObservableProperty] ColorPair primaryDark;
    [ObservableProperty] ColorPair secondaryLight;
    [ObservableProperty] ColorPair secondaryMid;
    [ObservableProperty] ColorPair secondaryDark;
    [ObservableProperty] Color validationError;
    [ObservableProperty] Color background;
    [ObservableProperty] Color paper;
    [ObservableProperty] Color cardBackground;
    [ObservableProperty] Color toolBarBackground;
    [ObservableProperty] Color body;
    [ObservableProperty] Color bodyLight;
    [ObservableProperty] Color columnHeader;
    [ObservableProperty] Color checkBoxOff;
    [ObservableProperty] Color checkBoxDisabled;
    [ObservableProperty] Color divider;
    [ObservableProperty] Color selection;
    [ObservableProperty] Color toolForeground;
    [ObservableProperty] Color toolBackground;
    [ObservableProperty] Color flatButtonClick;
    [ObservableProperty] Color flatButtonRipple;
    [ObservableProperty] Color toolTipBackground;
    [ObservableProperty] Color chipBackground;
    [ObservableProperty] Color snackbarBackground;
    [ObservableProperty] Color snackbarMouseOver;
    [ObservableProperty] Color snackbarRipple;
    [ObservableProperty] Color textBoxBorder;
    [ObservableProperty] Color textFieldBoxBackground;
    [ObservableProperty] Color textFieldBoxHoverBackground;
    [ObservableProperty] Color textFieldBoxDisabledBackground;
    [ObservableProperty] Color textAreaBorder;
    [ObservableProperty] Color textAreaInactiveBorder;
    [ObservableProperty] Color dataGridRowHoverBackground;
}
