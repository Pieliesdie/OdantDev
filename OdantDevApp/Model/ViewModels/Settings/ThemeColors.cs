using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using OdantDev;

namespace OdantDevApp.Model.ViewModels.Settings;

#pragma warning disable CS0657 // Not a valid attribute location for this declaration
[SuppressMessage("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "MVVMTK0034:Direct field reference to [ObservableProperty] backing field")]
public partial class ThemeColors : ObservableObject, ITheme
{
    private static Color ToColor(string value)
    {
        return (Color)(ColorConverter.ConvertFromString(value) ?? Colors.Black);
    }
    private static string FromColor(Color value)
    {
        return value.ToString();
    }
    private static string FromColorPair(ColorPair value)
    {
        return FromColor(value.Color);
    }
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
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair primaryLight;
    [XmlElement("PrimaryLight")]
    public string PrimaryLightXmlSurrogate
    {
        get => FromColorPair(primaryLight);
        set => primaryLight = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair primaryMid;
    [XmlElement("PrimaryMid")]
    public string PrimaryMidXmlSurrogate
    {
        get => FromColorPair(primaryMid);
        set => primaryMid = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair primaryDark;
    [XmlElement("PrimaryDark")]
    public string PrimaryDarkXmlSurrogate
    {
        get => FromColorPair(primaryDark);
        set => primaryDark = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair secondaryLight;
    [XmlElement("SecondaryLight")]
    public string SecondaryLightXmlSurrogate
    {
        get => FromColorPair(secondaryLight);
        set => secondaryLight = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair secondaryMid;
    [XmlElement("SecondaryMid")]
    public string SecondaryMidXmlSurrogate
    {
        get => FromColorPair(secondaryMid);
        set => secondaryMid = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private ColorPair secondaryDark;
    [XmlElement("SecondaryDark")]
    public string SecondaryDarkXmlSurrogate
    {
        get => FromColorPair(secondaryDark);
        set => secondaryDark = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color validationError;
    [XmlElement("ValidationError")]
    public string ValidationErrorXmlSurrogate
    {
        get => FromColor(validationError);
        set => validationError = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color background;
    [XmlElement("Background")]
    public string BackgroundXmlSurrogate
    {
        get => FromColor(background);
        set => background = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color paper;
    [XmlElement("Paper")]
    public string PaperXmlSurrogate
    {
        get => FromColor(paper);
        set => paper = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color cardBackground;
    [XmlElement("CardBackground")]
    public string CardBackgroundXmlSurrogate
    {
        get => FromColor(cardBackground);
        set => cardBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color toolBarBackground;
    [XmlElement("ToolBarBackground")]
    public string ToolBarBackgroundXmlSurrogate
    {
        get => FromColor(toolBarBackground);
        set => toolBarBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color body;
    [XmlElement("Body")]
    public string BodyXmlSurrogate
    {
        get => FromColor(body);
        set => body = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color bodyLight;
    [XmlElement("BodyLight")]
    public string BodyLightXmlSurrogate
    {
        get => FromColor(bodyLight);
        set => bodyLight = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color columnHeader;
    [XmlElement("ColumnHeader")]
    public string ColumnHeaderXmlSurrogate
    {
        get => FromColor(columnHeader);
        set => columnHeader = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color checkBoxOff;
    [XmlElement("CheckBoxOff")]
    public string CheckBoxOffXmlSurrogate
    {
        get => FromColor(checkBoxOff);
        set => checkBoxOff = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color checkBoxDisabled;
    [XmlElement("CheckBoxDisabled")]
    public string CheckBoxDisabledXmlSurrogate
    {
        get => FromColor(checkBoxDisabled);
        set => checkBoxDisabled = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color divider;
    [XmlElement("Divider")]
    public string DividerXmlSurrogate
    {
        get => FromColor(divider);
        set => divider = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color selection;
    [XmlElement("Selection")]
    public string SelectionXmlSurrogate
    {
        get => FromColor(selection);
        set => selection = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color toolForeground;
    [XmlElement("ToolForeground")]
    public string ToolForegroundXmlSurrogate
    {
        get => FromColor(toolForeground);
        set => toolForeground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color toolBackground;
    [XmlElement("ToolBackground")]
    public string ToolBackgroundXmlSurrogate
    {
        get => FromColor(toolBackground);
        set => toolBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color flatButtonClick;
    [XmlElement("FlatButtonClick")]
    public string FlatButtonClickXmlSurrogate
    {
        get => FromColor(flatButtonClick);
        set => flatButtonClick = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color flatButtonRipple;
    [XmlElement("FlatButtonRipple")]
    public string FlatButtonRippleXmlSurrogate
    {
        get => FromColor(flatButtonRipple);
        set => flatButtonRipple = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color toolTipBackground;
    [XmlElement("ToolTipBackground")]
    public string ToolTipBackgroundXmlSurrogate
    {
        get => FromColor(toolTipBackground);
        set => toolTipBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color chipBackground;
    [XmlElement("ChipBackground")]
    public string ChipBackgroundXmlSurrogate
    {
        get => FromColor(chipBackground);
        set => chipBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color snackbarBackground;
    [XmlElement("SnackbarBackground")]
    public string SnackbarBackgroundXmlSurrogate
    {
        get => FromColor(snackbarBackground);
        set => snackbarBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color snackbarMouseOver;
    [XmlElement("SnackbarMouseOver")]
    public string SnackbarMouseOverXmlSurrogate
    {
        get => FromColor(snackbarMouseOver);
        set => snackbarMouseOver = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color snackbarRipple;
    [XmlElement("SnackbarRipple")]
    public string SnackbarRippleXmlSurrogate
    {
        get => FromColor(snackbarRipple);
        set => snackbarRipple = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textBoxBorder;
    [XmlElement("TextBoxBorder")]
    public string TextBoxBorderXmlSurrogate
    {
        get => FromColor(textBoxBorder);
        set => textBoxBorder = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textFieldBoxBackground;
    [XmlElement("TextFieldBoxBackground")]
    public string TextFieldBoxBackgroundXmlSurrogate
    {
        get => FromColor(textFieldBoxBackground);
        set => textFieldBoxBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textFieldBoxHoverBackground;
    [XmlElement("TextFieldBoxHoverBackground")]
    public string TextFieldBoxHoverBackgroundXmlSurrogate
    {
        get => FromColor(textFieldBoxHoverBackground);
        set => textFieldBoxHoverBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textFieldBoxDisabledBackground;
    [XmlElement("TextFieldBoxDisabledBackground")]
    public string TextFieldBoxDisabledBackgroundXmlSurrogate
    {
        get => FromColor(textFieldBoxDisabledBackground);
        set => textFieldBoxDisabledBackground = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textAreaBorder;
    [XmlElement("TextAreaBorder")]
    public string TextAreaBorderXmlSurrogate
    {
        get => FromColor(textAreaBorder);
        set => textAreaBorder = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color textAreaInactiveBorder;
    [XmlElement("TextAreaInactiveBorder")]
    public string TextAreaInactiveBorderXmlSurrogate
    {
        get => FromColor(textAreaInactiveBorder);
        set => textAreaInactiveBorder = ToColor(value);
    }
    [property: XmlIgnore] [ObservableProperty]
    private Color dataGridRowHoverBackground;
    [XmlElement("DataGridRowHoverBackground")]
    public string DataGridRowHoverBackgroundXmlSurrogate
    {
        get => FromColor(dataGridRowHoverBackground);
        set => dataGridRowHoverBackground = ToColor(value);
    }
}