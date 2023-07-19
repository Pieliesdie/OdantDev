using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace OdantDev;

public static class BitmapEx
{
    public static BitmapImage ConvertToBitmapImage(this Bitmap src)
    {
        if (src == null) return null;
        using MemoryStream ms = new();
        src.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        ms.Seek(0, SeekOrigin.Begin);
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
    public static string ToBase64String(this BitmapImage src)
    {
        if (src == null) return null;
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(src);
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static BitmapImage FromBase64String(this string src)
    {
        if (src == null) return null;
        byte[] binaryData = Convert.FromBase64String(src);
        using MemoryStream ms = new(binaryData);
        ms.Seek(0, SeekOrigin.Begin);
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static BitmapImage ToImage(this byte[] array)
    {
        using var ms = new MemoryStream(array);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // here
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
