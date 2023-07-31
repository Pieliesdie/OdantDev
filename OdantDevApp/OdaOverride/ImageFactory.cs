using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Extensions.Caching.Memory;

using oda;

using OdantDev;

namespace SharedOdanDev.OdaOverride;

public static class PredefinedImages
{
    public static BitmapImage FolderImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.Folder)).ConvertToBitmapImage();
    public static BitmapImage WorkplaceImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage();
    public static BitmapImage ModuleImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage();
    public static BitmapImage LoadImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.Clock)).ConvertToBitmapImage();
    public static BitmapImage GitProject { get; } = Images.GetImage(Images.GetImageIndex(Icons.Method)).ConvertToBitmapImage();
}

public static class ImageFactory
{
    static readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 1500,
    });

    static readonly MemoryCacheEntryOptions _defaultCacheOptions = new MemoryCacheEntryOptions().SetSize(1);

    static readonly SemaphoreSlim _semaphoreSlimGetImageIndex = new(1);
    public static async Task<int> GetImageIndexAsync(this Item item)
    {
        await _semaphoreSlimGetImageIndex.WaitAsync();
        var ImageIndex = item.ImageIndex;
        _semaphoreSlimGetImageIndex.Release();
        return ImageIndex;
    }

    static readonly SemaphoreSlim _semaphoreSlimGetImage = new(1);
    public static async Task<Bitmap> GetBitmapAsync(int idx)
    {
        await _semaphoreSlimGetImage.WaitAsync();
        var image = new Bitmap(Images.GetImage(idx));
        _semaphoreSlimGetImage.Release();
        return image;
    }

    public static async Task<BitmapSource> GetImageSourceAsync(this Item item)
    {
        return await Task.Run(async () =>
        {
            var ImageIndex = await GetImageIndexAsync(item);
            if (_cache.TryGetValue<BitmapSource>(ImageIndex, out var cacheimg))
            {
                return cacheimg;
            }
            return await GetImageSourceAsync(ImageIndex);
        });
    }

    public static async Task<BitmapSource> GetImageSourceAsync(int idx)
    {
        if (_cache.TryGetValue<BitmapSource>(idx, out var cacheimg))
        {
            return cacheimg;
        }
        var image = await GetBitmapAsync(idx);
        var bitmapImg = image.ConvertToBitmapImage();
        _cache.Set(idx, bitmapImg, _defaultCacheOptions);
        return bitmapImg;
    }

    public static async Task<BitmapSource> GetImageSourceAsync(this Icons icon)
    {
        var idx = Images.GetImageIndex(icon);
        return await GetImageSourceAsync(idx);
    }
}
