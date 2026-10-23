using System.Windows.Media;
using System.Windows;
using FloatingWords.Mvvm;
using FloatingWords.Settings;

namespace FloatingWords.Design;

/// <summary>
/// 仅供 XAML 设计器预览使用。
/// 运行时的 DataContext 是 <see cref="SettingsWindowViewModel"/>，它需要 AppHost；
/// 设计器构造不出来，四个分区的 Visibility 绑定会全部失效并叠在一起。
/// <para>
/// 这些属性必须是<b>实例</b>属性和自动属性：WPF 不解析 static 成员，
/// 表达式体常量属性又会被 CA1822 判为“可标记为 static”。
/// </para>
/// </summary>
public sealed class SettingsWindowDesignData
{
    public SettingsWindowDesignData()
    {
        Sections =
        [
            new SettingsSectionItem("阅读", SettingsSection.Reading),
            new SettingsSectionItem("词书", SettingsSection.Lists),
            new SettingsSectionItem("导入", SettingsSection.Import),
            new SettingsSectionItem("样式", SettingsSection.Styles)
        ];

        SelectedSection = Sections[0];
    }

    public IReadOnlyList<SettingsSectionItem> Sections { get; }

    public SettingsSectionItem SelectedSection { get; set; }

    public bool IsReadingSelected { get; init; } = true;

    public bool IsListsSelected { get; init; }

    public bool IsImportSelected { get; init; }

    public bool IsStylesSelected { get; init; }

    public ReadingSectionDesignData Reading { get; } = new();

    public ListsSectionDesignData Lists { get; } = new();

    public ImportSectionDesignData Import { get; } = new();

    public StylesSectionDesignData Styles { get; } = new();
}

public sealed class ReadingSectionDesignData
{
    public bool IsFlashMode { get; set; } = true;

    public bool IsClickMode { get; set; }

    public double IntervalSeconds { get; set; } = 6;

    public string IntervalText { get; init; } = "每 6 秒";

    public bool PauseOnHover { get; set; } = true;

    public bool ShowExamples { get; set; }

    public string Summary { get; init; } = "当前为 Flash 定时闪卡 · 每 6 秒 · 悬停暂停已开启";

    public string ErrorMessage { get; init; } = string.Empty;

    public bool HasError { get; init; }
}

public sealed class ListsSectionDesignData
{
    public RelayCommand RefreshCommand { get; } = new(() => { });

    public RelayCommand OpenFolderCommand { get; } = new(() => { });

    public RelayCommand SelectCommand { get; } = new(_ => { });

    public RelayCommand RenameCommand { get; } = new(_ => { });

    public RelayCommand DeleteCommand { get; } = new(_ => { });

    public string CountText { get; init; } = "共 2 本词书";

    public string StatusMessage { get; init; } = string.Empty;

    public bool HasStatus { get; init; }

    public bool HasNoLists { get; init; }

    public IReadOnlyList<VocabularyListDesignItem> Lists { get; } =
    [
        new VocabularyListDesignItem
        {
            Name = "浮光词集",
            IsCurrent = true,
            CountsText = "320 词 · 486 义项",
            ListIdText = "listId: starter",
            SourceText = "来源：内置种子词书",
            ImportedLine = "导入于 2026-10-20 14:03"
        },
        new VocabularyListDesignItem
        {
            Name = "CET-6 高频",
            CountsText = "960 词 · 1,204 义项",
            ListIdText = "listId: cet6-core",
            SourceText = "来源：cet6.json"
        }
    ];
}

public sealed class VocabularyListDesignItem
{
    public string Name { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public string CountsText { get; init; } = string.Empty;

    public string ListIdText { get; init; } = string.Empty;

    public string SourceText { get; init; } = string.Empty;

    public string ImportedLine { get; init; } = string.Empty;
}

public sealed class ImportSectionDesignData
{
    public RelayCommand ChooseFileCommand { get; } = new(() => { });

    public RelayCommand ImportCommand { get; } = new(() => { });

    public string ImportButtonText { get; init; } = "导入";

    public bool CanImport { get; init; }

    public bool HasPreview { get; init; } = true;

    public bool HasIssues { get; init; }

    public string FileName { get; init; } = "cet6-core.json";

    public string FileSizeText { get; init; } = "184 KB";

    public string SourcePath { get; init; } = @"D:\词书\cet6-core.json";

    public string ListName { get; init; } = "CET-6 高频";

    public string ListIdText { get; init; } = "listId: cet6-core";

    public string CountsText { get; init; } = "960 词 · 1,204 义项";

    public IReadOnlyList<string> Issues { get; } = [];

    public string StatusMessage { get; init; } = string.Empty;

    public bool HasStatus { get; init; }
}

public sealed class StylesSectionDesignData
{
    public RelayCommand ResetCommand { get; } = new(() => { });

    public string StatusMessage { get; init; } = string.Empty;

    public bool HasStatus { get; init; }

    public IReadOnlyList<StyleRoleDesignItem> Roles { get; } =
    [
        new StyleRoleDesignItem
        {
            DisplayName = "单词",
            Role = "headword",
            SampleText = "serendipity",
            PreviewFontFamily = new FontFamily("Georgia"),
            PreviewFontSize = 26,
            PreviewFontWeight = FontWeights.Bold,
            PreviewForeground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
            FontFamily = "Georgia",
            FontSizeText = "26",
            FontWeight = "Bold",
            Color = "#FFFFFF"
        },
        new StyleRoleDesignItem
        {
            DisplayName = "释义",
            Role = "definition",
            SampleText = "漂浮；浮动",
            PreviewFontFamily = new FontFamily("Microsoft YaHei UI"),
            PreviewFontSize = 16,
            PreviewForeground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF3, 0xFA)),
            FontFamily = "Microsoft YaHei UI",
            FontSizeText = "16",
            FontWeight = "Normal",
            Color = "#F0F3FA"
        },
        new StyleRoleDesignItem
        {
            DisplayName = "标签",
            Role = "label",
            IsLabel = true,
            SampleText = "核心",
            PreviewFontFamily = new FontFamily("Segoe UI"),
            PreviewFontSize = 10.5,
            PreviewForeground = new SolidColorBrush(Color.FromRgb(0xC9, 0xF1, 0xDC)),
            PreviewBackground = new SolidColorBrush(Color.FromRgb(0x25, 0x46, 0x38)),
            FontFamily = "Segoe UI",
            FontSizeText = "10.5",
            FontWeight = "Normal",
            Color = "#C9F1DC",
            BackgroundColor = "#254638"
        }
    ];
}

public sealed class StyleRoleDesignItem
{
    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string SampleText { get; init; } = string.Empty;

    public System.Windows.Media.FontFamily PreviewFontFamily { get; init; } = new("Segoe UI");

    public double PreviewFontSize { get; init; } = 13;

    public System.Windows.FontWeight PreviewFontWeight { get; init; } = System.Windows.FontWeights.Normal;

    public FontStyle PreviewFontStyle { get; init; } = FontStyles.Normal;

    public Brush PreviewForeground { get; init; } = Brushes.White;

    public Brush PreviewBackground { get; init; } = Brushes.Transparent;

    public IReadOnlyList<string> FontFamilies { get; } = ["Segoe UI", "Georgia", "Microsoft YaHei UI"];

    public IReadOnlyList<string> FontWeights { get; } = ["Normal", "Medium", "SemiBold", "Bold"];

    public string FontFamily { get; set; } = "Segoe UI";

    public string FontSizeText { get; set; } = "13";

    public string FontWeight { get; set; } = "Normal";

    public bool Italic { get; set; }

    public string Color { get; set; } = "#F0F3FA";

    public bool IsLabel { get; init; }

    public string BackgroundColor { get; set; } = "#254638";

    public string Error { get; init; } = string.Empty;

    public bool HasError { get; init; }
}
