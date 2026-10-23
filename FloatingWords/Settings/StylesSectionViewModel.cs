using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using FloatingWords.Application.Settings;
using FloatingWords.Mvvm;
using FloatingWords.Visuals;

namespace FloatingWords.Settings;

public sealed class StylesSectionViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private string? _statusMessage;

    public StylesSectionViewModel(SettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        ResetCommand = new RelayCommand(Reset);

        Reload();
    }

    public static IReadOnlyList<string> FontFamilyOptions { get; } =
    [
        "Segoe UI",
        "Microsoft YaHei UI",
        "Georgia",
        "Times New Roman",
        "Cambria",
        "Arial",
        "Consolas"
    ];

    public static IReadOnlyList<string> FontWeightOptions { get; } =
    [
        "Normal",
        "Medium",
        "SemiBold",
        "Bold"
    ];

    public ObservableCollection<StyleRoleViewModel> Roles { get; } = [];

    public RelayCommand ResetCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage ?? string.Empty;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    private void Reload()
    {
        Roles.Clear();

        Dictionary<string, TextStyle> map = _settings.Current.StyleMap;

        foreach (string role in TextRoles.All)
        {
            TextStyle style = map.TryGetValue(role, out TextStyle? configured)
                ? configured
                : SettingsDefaults.CreateDefaultStyle(role);

            Roles.Add(new StyleRoleViewModel(role, style, Save));
        }
    }

    private void Save(StyleRoleViewModel role)
    {
        string? error = role.Validate();

        if (!string.IsNullOrWhiteSpace(error))
        {
            role.Error = error;
            return;
        }

        role.Error = string.Empty;

        try
        {
            TextStyle style = role.ToStyle();
            _settings.Update(settings => settings.StyleMap[role.Role] = style);
            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            role.Error = "保存失败：" + exception.Message;
        }
    }

    private void Reset()
    {
        try
        {
            _settings.Update(settings => settings.StyleMap = SettingsDefaults.CreateStyleMap());
            Reload();
            StatusMessage = "已恢复默认样式。";
        }
        catch (Exception exception)
        {
            StatusMessage = "恢复失败：" + exception.Message;
        }
    }
}

public sealed class StyleRoleViewModel : ObservableObject
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

    private readonly Action<StyleRoleViewModel> _save;

    private string _fontFamily;
    private string _fontSizeText;
    private string _fontWeight;
    private string _color;
    private string _backgroundColor;
    private bool _italic;
    private string _error = string.Empty;

    public StyleRoleViewModel(string role, TextStyle style, Action<StyleRoleViewModel> save)
    {
        Role = role;
        _save = save ?? throw new ArgumentNullException(nameof(save));

        _fontFamily = style.FontFamily;
        _fontSizeText = style.FontSize.ToString("0.#", CultureInfo.InvariantCulture);
        _fontWeight = style.FontWeight;
        _color = style.Color;
        _backgroundColor = style.BackgroundColor ?? "#254638";
        _italic = style.Italic;
    }

    public string Role { get; }

    public string DisplayName => Role switch
    {
        TextRoles.Headword => "单词",
        TextRoles.Phonetic => "音标",
        TextRoles.PartOfSpeech => "词性",
        TextRoles.Definition => "释义",
        TextRoles.DefinitionTranslation => "释义翻译",
        TextRoles.Example => "例句",
        TextRoles.ExampleTranslation => "例句翻译",
        TextRoles.Label => "标签",
        _ => Role
    };

    public string SampleText => Role switch
    {
        TextRoles.Headword => "serendipity",
        TextRoles.Phonetic => "/ˌserənˈdɪpəti/",
        TextRoles.PartOfSpeech => "v.",
        TextRoles.Definition => "漂浮；浮动",
        TextRoles.DefinitionTranslation => "to stay on the surface",
        TextRoles.Example => "Leaves float on the water.",
        TextRoles.ExampleTranslation => "叶子漂浮在水面上。",
        TextRoles.Label => "核心",
        _ => Role
    };

    public bool IsLabel => string.Equals(Role, TextRoles.Label, StringComparison.Ordinal);

    public IReadOnlyList<string> FontFamilies { get; } = StylesSectionViewModel.FontFamilyOptions;

    public IReadOnlyList<string> FontWeights { get; } = StylesSectionViewModel.FontWeightOptions;

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (SetProperty(ref _fontFamily, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public string FontSizeText
    {
        get => _fontSizeText;
        set
        {
            if (SetProperty(ref _fontSizeText, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public string FontWeight
    {
        get => _fontWeight;
        set
        {
            if (SetProperty(ref _fontWeight, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public string Color
    {
        get => _color;
        set
        {
            if (SetProperty(ref _color, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (SetProperty(ref _backgroundColor, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public bool Italic
    {
        get => _italic;
        set
        {
            if (SetProperty(ref _italic, value))
            {
                RaisePreview();
                _save(this);
            }
        }
    }

    public string Error
    {
        get => _error;
        set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public FontFamily PreviewFontFamily => UiBrushes.ParseFontFamily(_fontFamily);

    public double PreviewFontSize => TryGetFontSize(out double size) ? size : 14;

    public FontWeight PreviewFontWeight => UiBrushes.ParseFontWeight(_fontWeight);

    public FontStyle PreviewFontStyle => _italic ? FontStyles.Italic : FontStyles.Normal;

    public Brush PreviewForeground => UiBrushes.FromHex(_color);

    public Brush PreviewBackground => IsLabel ? UiBrushes.FromHex(_backgroundColor) : Brushes.Transparent;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(_fontFamily))
        {
            return "字体族不能为空。";
        }

        if (!TryGetFontSize(out double size))
        {
            return "字号必须是 9 到 72 之间的数字。";
        }

        if (!SettingsDefaults.IsValidColor(_color))
        {
            return "文字颜色必须是 #RRGGBB 或 #AARRGGBB。";
        }

        if (IsLabel && !SettingsDefaults.IsValidColor(_backgroundColor))
        {
            return "标签背景必须是 #RRGGBB 或 #AARRGGBB。";
        }

        return null;
    }

    public TextStyle ToStyle() => new()
    {
        FontFamily = _fontFamily.Trim(),
        FontSize = TryGetFontSize(out double size) ? size : 14,
        FontWeight = ValidFontWeights.Contains(_fontWeight) ? _fontWeight : "Normal",
        Color = _color.Trim().ToUpperInvariant(),
        Italic = _italic,
        BackgroundColor = IsLabel ? _backgroundColor.Trim().ToUpperInvariant() : null
    };

    private bool TryGetFontSize(out double size) =>
        double.TryParse(_fontSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out size) &&
        double.IsFinite(size) &&
        size is >= 9 and <= 72;

    private void RaisePreview()
    {
        OnPropertyChanged(nameof(PreviewFontFamily));
        OnPropertyChanged(nameof(PreviewFontSize));
        OnPropertyChanged(nameof(PreviewFontWeight));
        OnPropertyChanged(nameof(PreviewFontStyle));
        OnPropertyChanged(nameof(PreviewForeground));
        OnPropertyChanged(nameof(PreviewBackground));
    }
}
