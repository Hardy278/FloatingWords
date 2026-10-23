using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FloatingWords.Visuals;

/// <summary>
/// 卡片质感的共享实现：噪点贴图与圆角裁剪。
/// 主窗口、设置窗口、输入弹窗共用同一套外观，避免各写一份。
/// </summary>
public static class CardChrome
{
    private const int NoiseTileSize = 128;
    private const double ClipCornerRadius = 23;

    /// <summary>给指定矩形铺上可平铺的噪点，模拟磨砂玻璃的细微颗粒。</summary>
    public static void ApplyNoise(Rectangle target)
    {
        ArgumentNullException.ThrowIfNull(target);

        byte[] pixels = new byte[NoiseTileSize * NoiseTileSize * 4];
        var random = new Random(20261022);

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            byte value = (byte)random.Next(100, 256);
            byte alpha = (byte)random.Next(0, 18);

            pixels[offset] = value;
            pixels[offset + 1] = value;
            pixels[offset + 2] = value;
            pixels[offset + 3] = alpha;
        }

        BitmapSource bitmap = BitmapSource.Create(
            NoiseTileSize,
            NoiseTileSize,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            NoiseTileSize * 4);

        bitmap.Freeze();

        var noise = new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, NoiseTileSize, NoiseTileSize)
        };

        noise.Freeze();

        target.Fill = noise;
        RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.NearestNeighbor);
    }

    /// <summary>把元素裁成与卡片一致的圆角，避免光晕和噪点溢出到圆角外。</summary>
    public static void ApplyClip(FrameworkElement target, Size size)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        target.Clip = new RectangleGeometry(
            new Rect(0, 0, size.Width, size.Height),
            ClipCornerRadius,
            ClipCornerRadius);
    }
}
