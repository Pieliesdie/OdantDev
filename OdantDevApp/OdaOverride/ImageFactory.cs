using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using oda;

using OdantDev;

namespace SharedOdanDev.OdaOverride;

public static class ImageFactory
{
    public static BitmapImage FolderImage = Images.GetImage(Images.GetImageIndex(Icons.Folder)).ConvertToBitmapImage();
    public static BitmapImage WorkplaceImage = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage();
    public static BitmapImage ModuleImage = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage();

    static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

    public static async Task<int> GetImageIndex(this Item item)
    {
        await _semaphoreSlim.WaitAsync();
        var ImageIndex = item.ImageIndex;
        _semaphoreSlim.Release();
        return ImageIndex;
    }

    public static async Task<Bitmap> GetImage(this int idx)
    {
        await _semaphoreSlim.WaitAsync();
        var image = new Bitmap(Images.GetImage(idx));
        _semaphoreSlim.Release();
        return image;
    }

    public static async Task<BitmapImage> GetImageSource(this Item item)
    {
        return await Task.Run(async () =>
        {
            await _semaphoreSlim.WaitAsync();
            var ImageIndex = item.ImageIndex;
            var image = new Bitmap(Images.GetImage(ImageIndex));
            _semaphoreSlim.Release();
            return image.ConvertToBitmapImage();
        });
    }
}
