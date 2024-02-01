using System.Windows;
using System.Windows.Media;

namespace MaterialDesign.PropertyGrid.Implementations;

public static class VisualTreeHelperEx
{
    public static Visual FindParentOfType<T>(this DependencyObject source)
    {
        while (source != null && source.GetType() != typeof(T))
        {
            source = VisualTreeHelper.GetParent(source);
        }

        return source as Visual;
    }
}
