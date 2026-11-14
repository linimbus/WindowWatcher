using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace WindowWatcher;

public sealed class OcrService
{
    private OcrEngine? _engine;
    private string _languageTag = "";

    public static IReadOnlyList<string> AvailableLanguages
    {
        get
        {
            try
            {
                return OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    public void EnsureEngine(string languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            languageTag = AvailableLanguages.FirstOrDefault() ?? "en-US";
        }

        if (_engine != null && string.Equals(_languageTag, languageTag, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var engine = OcrEngine.TryCreateFromLanguage(new Language(languageTag));
        if (engine == null)
        {
            throw new InvalidOperationException(
                $"无法创建 OCR 引擎，语言“{languageTag}”不可用。请在 Windows 设置中安装对应的 OCR 语言包。");
        }

        _engine = engine;
        _languageTag = languageTag;
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap, double scale)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("OCR 引擎尚未初始化。");
        }

        var effectiveScale = Math.Max(1.0, scale);
        var maxDimension = (double)OcrEngine.MaxImageDimension;
        effectiveScale = Math.Min(effectiveScale, Math.Min(maxDimension / bitmap.Width, maxDimension / bitmap.Height));

        using var scaled = effectiveScale > 1.001 ? Scale(bitmap, effectiveScale) : null;
        var source = scaled ?? bitmap;
        using var softwareBitmap = ToSoftwareBitmap(source);
        var result = await _engine.RecognizeAsync(softwareBitmap);
        return result.Text ?? "";
    }

    private static Bitmap Scale(Bitmap source, double scale)
    {
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(source, 0, 0, width, height);
        return result;
    }

    private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
    {
        Bitmap working = bitmap;
        Bitmap? converted = null;
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
        {
            converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(converted);
            graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            working = converted;
        }

        try
        {
            var width = working.Width;
            var height = working.Height;
            var rowBytes = width * 4;
            var pixels = new byte[rowBytes * height];

            var data = working.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * rowBytes, rowBytes);
                }
            }
            finally
            {
                working.UnlockBits(data);
            }

            for (var i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }

            var buffer = CryptographicBuffer.CreateFromByteArray(pixels);
            return SoftwareBitmap.CreateCopyFromBuffer(
                buffer, BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
        }
        finally
        {
            converted?.Dispose();
        }
    }
}
