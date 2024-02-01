using System.Windows;

using MaterialDesign.PropertyGrid.Model;

namespace MaterialDesign.PropertyGrid.Implementations;

/// <summary>
/// Defines a <see cref="PropertyEditor"/> used in <see cref="PropertyGrid"/>.
/// </summary>
internal class PropertyEditor(FrameworkElement editor, PropertyField field)
{
    public FrameworkElement VisualEditor { get; private set; } = editor;
    public PropertyField Field { get; private set; } = field;
}
