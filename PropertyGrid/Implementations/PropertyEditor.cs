
#region Namespace Imports

using System;
using System.Windows;
using System.Windows.Controls;

#endregion

namespace Vasu.Wpf.Controls
{
    /// <summary>
    /// Defines a <see cref="PropertyEditor"/> used in <see cref="PropertyGrid"/>.
    /// </summary>
    internal class PropertyEditor(FrameworkElement editor, PropertyField field)
    {
        public FrameworkElement VisualEditor { get; private set; } = editor;
        public PropertyField Field { get; private set; } = field;
    }
}
