namespace FloatingWords.Application.Settings;

public static class SettingsDefaults
{
    private static readonly HashSet<string> ValidFontWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        "Thin",
        "ExtraLight",
        "Light",
        "Normal",
        "Medium",
        "SemiBold",
        "Bold",
        "ExtraBold",
        "Black"
    };

    public static AppSettings Create() => new()
    {
        SchemaVersion = 1,
        CurrentListId = null,
        ReadingMode = ReadingMode.Flash,
        RotationIntervalSeconds = 5,
        PauseOnHover = true,
        ShowExamples = false,
        Window = new WindowPlacement(),
        StyleMap = CreateStyleMap()
    };

    public static Dictionary<string, TextStyle> CreateStyleMap()
    {
        var map = new Dictionary<string, TextStyle>(StringComparer.Ordinal);

        foreach (string role in TextRoles.All)
        {
            map[role] = CreateDefaultStyle(role);
        }

        return map;
    }

    public static TextStyle CreateDefaultStyle(string role) => role switch
    {
        TextRoles.Headword => new TextStyle
        {
            FontFamily = "Georgia",
            FontSize = 26,
            FontWeight = "Bold",
            Color = "#FFFFFF"
        },
        TextRoles.Phonetic => new TextStyle
        {
            FontFamily = "Segoe UI",
            FontSize = 13,
            FontWeight = "Normal",
            Color = "#B8C4D9",
            Italic = true
        },
        TextRoles.PartOfSpeech => new TextStyle
        {
            FontFamily = "Segoe UI",
            FontSize = 12,
            FontWeight = "SemiBold",
            Color = "#F0B57A",
            Italic = true
        },
        TextRoles.Definition => new TextStyle
        {
            FontFamily = "Microsoft YaHei UI",
            FontSize = 16,
            FontWeight = "Normal",
            Color = "#F0F3FA"
        },
        TextRoles.DefinitionTranslation => new TextStyle
        {
            FontFamily = "Microsoft YaHei UI",
            FontSize = 14,
            FontWeight = "Normal",
            Color = "#C7D0E0"
        },
        TextRoles.Example => new TextStyle
        {
            FontFamily = "Georgia",
            FontSize = 13,
            FontWeight = "Normal",
            Color = "#B5C0D3",
            Italic = true
        },
        TextRoles.ExampleTranslation => new TextStyle
        {
            FontFamily = "Microsoft YaHei UI",
            FontSize = 12,
            FontWeight = "Normal",
            Color = "#919EB5"
        },
        TextRoles.Label => new TextStyle
        {
            FontFamily = "Segoe UI",
            FontSize = 10.5,
            FontWeight = "Normal",
            Color = "#C9F1DC",
            BackgroundColor = "#254638"
        },
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "未知的文本角色。")
    };

    public static void Normalize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.SchemaVersion = 1;
        settings.CurrentListId = string.IsNullOrWhiteSpace(settings.CurrentListId) ? null : settings.CurrentListId.Trim();

        if (!Enum.IsDefined(settings.ReadingMode))
        {
            settings.ReadingMode = ReadingMode.Flash;
        }

        settings.RotationIntervalSeconds = Math.Clamp(settings.RotationIntervalSeconds, 1, 60);

        WindowPlacement window = settings.Window ?? new WindowPlacement();

        double left = double.IsFinite(window.Left) ? window.Left : 100;
        double top = double.IsFinite(window.Top) ? window.Top : 100;
        double width = double.IsFinite(window.Width)
            ? Math.Clamp(window.Width, 448, 4096)
            : 508;
        double height = double.IsFinite(window.Height)
            ? Math.Clamp(window.Height, 368, 4096)
            : 388;

        settings.Window = new WindowPlacement
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height
        };

        Dictionary<string, TextStyle> sourceStyles = settings.StyleMap;
        var normalizedStyles = new Dictionary<string, TextStyle>(StringComparer.Ordinal);
        foreach (string role in TextRoles.All)
        {
            sourceStyles.TryGetValue(role, out TextStyle? style);
            normalizedStyles[role] = NormalizeStyle(role, style);
        }
        settings.StyleMap = normalizedStyles;
    }

    public static bool IsValidColor(string? value)
    {
        if (value?.Length is not (7 or 9) || value[0] != '#')
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (!Uri.IsHexDigit(value[index]))
            {
                return false;
            }
        }
        return true;
    }

    private static TextStyle NormalizeStyle(string role, TextStyle? style)
    {
        TextStyle fallback = CreateDefaultStyle(role);

        if (style is null)
        {
            return fallback;
        }

        var normalized = new TextStyle
        {
            FontFamily = string.IsNullOrWhiteSpace(style.FontFamily)
                ? fallback.FontFamily
                : style.FontFamily.Trim(),

            FontSize = double.IsFinite(style.FontSize)
                ? Math.Clamp(style.FontSize, 9, 72)
                : fallback.FontSize,

            FontWeight = ValidFontWeights.Contains(style.FontWeight)
                ? style.FontWeight
                : fallback.FontWeight,

            Color = IsValidColor(style.Color)
                ? style.Color.ToUpperInvariant()
                : fallback.Color,

            Italic = style.Italic,

            BackgroundColor = string.Equals(role, TextRoles.Label, StringComparison.Ordinal)
                ? (IsValidColor(style.BackgroundColor)
                    ? style.BackgroundColor!.ToUpperInvariant()
                    : fallback.BackgroundColor)
                : null
        };

        return normalized;
    }
}
