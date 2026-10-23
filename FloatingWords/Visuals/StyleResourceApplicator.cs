using System.Windows;
using System.Windows.Media;
using FloatingWords.Application.Settings;

namespace FloatingWords.Visuals;

public static class StyleResourceApplicator
{
    public static string FontFamilyKey(string role) => $"Role_{role}_FontFamily";

    public static string FontSizeKey(string role) => $"Role_{role}_FontSize";

    public static string FontWeightKey(string role) => $"Role_{role}_FontWeight";

    public static string ForegroundKey(string role) => $"Role_{role}_Foreground";

    public static string FontStyleKey(string role) => $"Role_{role}_FontStyle";

    public static string BackgroundKey(string role) => $"Role_{role}_Background";

    public static void Apply(ResourceDictionary resources, IReadOnlyDictionary<string, TextStyle> styleMap)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(styleMap);

        foreach (string role in TextRoles.All)
        {
            TextStyle style = styleMap.TryGetValue(role, out TextStyle? configured) ? configured : SettingsDefaults.CreateDefaultStyle(role);

            resources[FontFamilyKey(role)] = UiBrushes.ParseFontFamily(style.FontFamily);
            resources[FontSizeKey(role)] = style.FontSize;
            resources[FontWeightKey(role)] = UiBrushes.ParseFontWeight(style.FontWeight);
            resources[ForegroundKey(role)] = UiBrushes.FromHex(style.Color);
            resources[FontStyleKey(role)] = style.Italic ? FontStyles.Italic : FontStyles.Normal;
            resources[BackgroundKey(role)] = role == TextRoles.Label ? UiBrushes.FromHex(style.BackgroundColor) : Brushes.Transparent;
        }
    }
}
