using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FloatingWords.Visuals;

public static class UiBrushes
{
    public static SolidColorBrush FromHex(string? hex, byte? alpha = null)
    {
        if (!TryParseColor(hex, out Color color))
        {
            color = Colors.Transparent;
        }

        if (alpha is not null)
        {
            color = Color.FromArgb(alpha.Value, color.R, color.G, color.B);
        }

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static bool TryParseColor(string? hex, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        string text = hex.Trim().TrimStart('#');

        if (text.Length == 6)
        {
            text = "FF" + text;
        }

        if (text.Length != 8 || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            return false;
        }

        color = Color.FromArgb(
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF));

        return true;
    }

    public static FontWeight ParseFontWeight(string? value) => value?.Trim() switch
    {
        "Thin" => FontWeights.Thin,
        "ExtraLight" => FontWeights.ExtraLight,
        "Light" => FontWeights.Light,
        "Medium" => FontWeights.Medium,
        "SemiBold" => FontWeights.SemiBold,
        "Bold" => FontWeights.Bold,
        "ExtraBold" => FontWeights.ExtraBold,
        "Black" => FontWeights.Black,
        _ => FontWeights.Normal
    };

    public static FontFamily ParseFontFamily(string? value) => new(string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value);
}
