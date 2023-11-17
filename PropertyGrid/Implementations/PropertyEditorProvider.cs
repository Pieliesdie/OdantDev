
#region Namespace Imports

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using ColorPicker;

using MaterialDesignColors;

using MaterialDesignThemes.Wpf;

#endregion

namespace Vasu.Wpf.Controls;

/// <summary>
/// Implements methods to decide visual editor for the <see cref="PropertyField"/>.
/// </summary>
internal static class PropertyEditorProvider
{
    public static PropertyEditor GetFieldEditor(PropertyField field)
    {
        if (field == null) return null;
        FrameworkElement element = Editor(field);
        return new PropertyEditor(element, field);
    }

    private static FrameworkElement Editor(PropertyField field)
    {
        var type = Nullable.GetUnderlyingType(field.PropertyType) ?? field.PropertyType;
        if (!type.IsPrimitiveType())
        {
            return NestedEditor(field);
        }
        if (!field.PropertyInfo.CanWrite)
        {
            return ReadOnlyEditor(field);
        }
        if (type.IsEnum)
        {
            return EnumEditor(field);
        }
        if (type == typeof(bool))
        {
            return BooleanEditor(field);
        }
        if (type == typeof(string))
        {
            return StringEditor(field);
        }
        if (type.IsNumericType())
        {
            return NumericEditor(field);
        }
        if (type == typeof(Color))
        {
            return ColorEditor(field);
        }
        return DefaultEditor(field);
    }

    private static FrameworkElement NestedEditor(PropertyField field)
    {
        var root = new Grid()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star)  },
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto)}
            }
        };
        var rowEditor = ReadOnlyEditor(field);
        rowEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
        rowEditor.Text = field.PropertyType.ToString();
        root.Children.Add(rowEditor);

        var edit = new PopupBox
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            PopupContent = new PropertyGrid() { SelectedObject = field.Value },
            StaysOpen = true
        };
        root.Children.Add(edit);
        Grid.SetColumn(rowEditor, 0);
        Grid.SetColumn(edit, 1);
        return root;
    }
    private static StandardColorPicker ColorEditor(PropertyField field)
    {
        var colorEditor = new StandardColorPicker
        {
            Foreground = Brushes.Gray,
            BorderThickness = new Thickness(0)
        };
        colorEditor.SetBinding(PickerControlBase.SelectedColorProperty, GetBinding(field));
        return colorEditor;
    }
    private static TextBox ReadOnlyEditor(PropertyField field)
    {
        var textEditor = StringEditor(field);
        textEditor.IsReadOnly = true;
        textEditor.Foreground = Brushes.Gray;
        return textEditor;
    }
    private static TextBox StringEditor(PropertyField field)
    {
        var textEditor = new TextBox
        {
            BorderThickness = new Thickness(0)
        };
        textEditor.SetBinding(TextBox.TextProperty, GetBinding(field));
        return textEditor;
    }
    private static TextBox NumericEditor(PropertyField field) => StringEditor(field);
    private static ComboBox EnumEditor(PropertyField field)
    {
        var comboEditor = new ComboBox
        {
            ItemsSource = Enum.GetValues(field.PropertyType),
            BorderThickness = new Thickness(0)
        };

        comboEditor.SetBinding(ComboBox.SelectedValueProperty, GetBinding(field));
        return comboEditor;
    }
    private static CheckBox BooleanEditor(PropertyField field)
    {
        var comboEditor = new CheckBox
        {
            BorderThickness = new Thickness(0),
            IsThreeState = field.PropertyType == typeof(bool?),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        comboEditor.SetBinding(CheckBox.IsCheckedProperty, GetBinding(field));
        return comboEditor;
    }
    private static TextBlock DefaultEditor(PropertyField field)
    {
        var textBlock = new TextBlock
        {
            Text = field.Value == null ? string.Empty : field.Value.ToString()
        };

        return textBlock;
    }

    private static Binding GetBinding(PropertyField field)
    {
        Binding bnding = new Binding(field.PropertyInfo.Name)
        {
            Source = field.SourceObject,
            Mode = field.PropertyDescriptor.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
        };
        return bnding;
    }

    private static bool IsPrimitiveType(this Type t) => t.IsPrimitive || t.IsValueType || (t == typeof(string));
    public static bool IsNumericType(this Type o)
    {
        switch (Type.GetTypeCode(o))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }
}
