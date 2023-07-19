using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SharedOdantDev.Model;
public abstract class BaseGitItem
{
    static readonly ConcurrentDictionary<string, ImageSource> _images = new();
    public static Pen Pen { get; set; } = new(Brushes.Black, 0.1);

    public static Brush Brush { get; set; } = Brushes.Black;

    public abstract string Name { get; }

    public abstract object Object { get; }

    public virtual string FullPath { get; protected set; }

    protected abstract string ImageCode { get; }
    public virtual ImageSource Icon
    {
        get
        {
            if (_images.TryGetValue(ImageCode, out ImageSource imageSource))
            {
                return imageSource;
            }

            return GetImageSource(ImageCode, Pen, Brush, null, null, null);
        }
    }

    public virtual bool HasModule { get; set; }

    private static ImageSource GetImageSource(string geometry, Pen pen, Brush brush, Transform transform, IValueConverter converter, object parameter)
    {
        var geom = new GeometryDrawing
        {
            Geometry = Geometry.Parse(geometry),
        };
        if (pen != null)
        {
            // geom.Pen = pen;
        }
        if (brush != null)
        {
            geom.Brush = brush;
        }
        var grp = new DrawingGroup
        {
            Transform = transform,
            Children = { geom },
        };
        ImageSource result = new DrawingImage
        {
            Drawing = grp,
        };
        if (converter != null)
        {
            result = (ImageSource)converter.Convert(result, typeof(ImageSource), parameter, CultureInfo.CurrentCulture);
        }
        return result;
    }
}
