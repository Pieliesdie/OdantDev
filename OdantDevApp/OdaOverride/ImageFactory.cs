using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using Microsoft.Extensions.Caching.Memory;

using oda;

using OdantDev;

namespace SharedOdanDev.OdaOverride;

public static class ImageFactory
{
    static readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 1500,
    });

    static readonly MemoryCacheEntryOptions _defaultCacheOptions = new MemoryCacheEntryOptions().SetSize(1);

    public static BitmapImage FolderImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.Folder)).ConvertToBitmapImage();
    public static BitmapImage WorkplaceImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage();
    public static BitmapImage ModuleImage { get; } = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage();

    static readonly SemaphoreSlim _semaphoreSlimGetImageIndex = new(1);
    public static async Task<int> GetImageIndex(this Item item)
    {
        await _semaphoreSlimGetImageIndex.WaitAsync();
        var ImageIndex = item.ImageIndex;
        _semaphoreSlimGetImageIndex.Release();
        return ImageIndex;
    }

    static readonly SemaphoreSlim _semaphoreSlimGetImage = new(1);
    public static async Task<Bitmap> GetImage(int idx)
    {
        await _semaphoreSlimGetImage.WaitAsync();
        var image = new Bitmap(Images.GetImage(idx));
        _semaphoreSlimGetImage.Release();
        return image;
    }

    public static async Task<BitmapImage> GetImageSource(this Item item)
    {
        return await Task.Run(async () =>
        {
            var ImageIndex = await GetImageIndex(item);
            if (_cache.TryGetValue<BitmapImage>(ImageIndex, out var cacheimg))
            {
                return cacheimg;
            }
            var image = await GetImage(ImageIndex);
            var bitmapImg = image.ConvertToBitmapImage();
            _cache.Set(ImageIndex, bitmapImg, _defaultCacheOptions);
            return bitmapImg;
        });
    }
}
