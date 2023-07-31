using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using Microsoft.Extensions.Caching.Memory;

using oda;

using OdantDev;
using OdaOverride;

namespace OdaOverride;

public static class ImageFactory
{
    static readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 500,
    });

    public static BitmapImage FolderImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.Folder)).ConvertToBitmapImage();
    public static BitmapImage WorkplaceImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage();
    public static BitmapImage ModuleImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage();

    static readonly SemaphoreSlim _semaphoreSlim = new(1);

    public static async Task<int> GetImageIndex(this Item item)
    {
        await _semaphoreSlim.WaitAsync();
        var ImageIndex = item.ImageIndex;
        _semaphoreSlim.Release();
        return ImageIndex;
    }

    public static async Task<Bitmap> GetImage(this int idx)
    {
        if (_cache.TryGetValue<Bitmap>(idx, out var cachedBitmap))
        {
            return cachedBitmap;
        }
        await _semaphoreSlim.WaitAsync();
        var image = new Bitmap(Images.GetImage(idx));
        _semaphoreSlim.Release();
        _cache.Set(idx, image);
        return image;
    }

    public static async Task<BitmapImage> GetImageSource(this Item item)
    {
        if (_cache.TryGetValue<BitmapImage>(item.FullId, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        var img = await Task.Run(async () =>
        {
            await _semaphoreSlim.WaitAsync();
            var ImageIndex = item.ImageIndex;
            var image = new Bitmap(Images.GetImage(ImageIndex));
            _semaphoreSlim.Release();
            return image.ConvertToBitmapImage();
        });
        _cache.Set(item.FullId, img);
        return img;
    }
}
