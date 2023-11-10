
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

namespace Vasu.Wpf.Controls
{
    /// <summary>
    /// Implements methods to decide visual editor for the <see cref="PropertyField"/>.
    /// </summary>
    internal static class PropertyEditorProvider
    {
        public static PropertyEditor GetFieldEditor(PropertyField field)
        {
            if (field == null) return null;
            FrameworkElement element;
            if (!field.PropertyType.IsPrimitiveType())
            {
                var root = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                var rowEditor = ReadOnlyEditor(field); 
                rowEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
                root.Children.Add(rowEditor);
                var popupEditor =  StringEditor(field);
                popupEditor.Height = 150;
                popupEditor.Width = 150;
                var edit = new PopupBox
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Content = new TextBlock() { Text = "123"}
                };
                root.Children.Add(edit);
                element = root;
            }
            else
            {
                element = Editor(field);
            }

            return new PropertyEditor(element, field);
        }

        private static FrameworkElement Editor(PropertyField field)
        {
            if (!field.PropertyInfo.CanWrite)
            {
                return ReadOnlyEditor(field);
            }
            if (field.PropertyType.IsEnum)
            {
                return EnumEditor(field);
            }
            if (field.PropertyType == typeof(bool))
            {
                return BooleanEditor(field);
            }
            if (field.PropertyType == typeof(string))
            {
                return StringEditor(field);
            }
            if (field.PropertyType.IsNumericType())
            {
                return NumericEditor(field);
            }
            if (field.PropertyType == typeof(Color))
            {
                return ColorEditor(field);
            }
            //if (!field.PropertyType.IsPrimitive)
            //{
            //    return new PropertyGrid() { SelectedObject = field.Value };
            //}

            return DefaultEditor(field);
        }
        private static StandardColorPicker ColorEditor(PropertyField field)
        {
            var colorEditor = new StandardColorPicker
            {
                Foreground = Brushes.Gray,
                BorderThickness = new Thickness(0)
            };
            colorEditor.SetBinding(StandardColorPicker.SelectedColorProperty, GetBinding(field));
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
            var comboEditor = new CheckBox();
            comboEditor.BorderThickness = new Thickness(0);
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
                Mode = field.PropertyDescriptor.IsReadOnly? BindingMode.OneWay : BindingMode.TwoWay
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
}
