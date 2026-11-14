using System.Drawing.Imaging;

namespace WindowWatcher;

public sealed class ScreenCaptureService
{
    public Bitmap Capture(Rectangle region)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException("截取区域尺寸无效。", nameof(region));
        }

        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }
}
