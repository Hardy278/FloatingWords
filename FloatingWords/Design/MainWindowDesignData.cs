using FloatingWords.Domain.Ratings;
using FloatingWords.Mvvm;
using FloatingWords.Reading;

namespace FloatingWords.Design;

/// <summary>
/// 仅供 XAML 设计器预览使用。
/// 运行时的 DataContext 是 <see cref="MainViewModel"/>，它需要 AppHost 才能构造；
/// 设计器构造不出来，绑定路径就无法解析（“DataContext 未知”），
/// 因此这里提供一份同形状、带示例数据的替身。
/// <para>
/// 这些属性必须是<b>实例</b>属性，且用自动属性（有后备字段）而不是表达式体常量——
/// WPF 数据绑定不解析 static 成员，而表达式体常量属性会被 CA1822 判为“可标记为 static”。
/// </para>
/// </summary>
public sealed class MainWindowDesignData
{
    public bool IsReady { get; init; }

    public bool IsReading { get; init; } = true;

    public bool IsEmpty { get; init; }

    public bool IsError { get; init; }

    public bool IsState { get; init; }

    public bool HasCurrentList { get; init; } = true;

    public bool HasNoCurrentList { get; init; }

    public bool HasLists { get; init; } = true;

    public string CurrentListName { get; init; } = "浮光词集";

    public string SummaryLine { get; init; } = "浮光词集 · Flash · 每 6 秒";

    public string HoverHint { get; init; } = "悬停暂停 · 评级不会打断节奏";

    public string StateMessage { get; init; } = "先把词书放进来，再开始。";

    public string ErrorMessage { get; init; } = string.Empty;

    public ReadingDesignData Reading { get; init; } = new();
}

public sealed class ReadingDesignData
{
    public string Headword { get; init; } = "float";

    public string PhoneticUk { get; init; } = "/fləʊt/";

    public bool ShowUk { get; init; } = true;

    public string PhoneticUs { get; init; } = "/floʊt/";

    public bool ShowUs { get; init; } = true;

    public IReadOnlyList<LabelChipViewModel> Labels { get; } =
    [
        new LabelChipViewModel("核心"),
        new LabelChipViewModel("雅思")
    ];

    /// <summary>
    /// 默认设置下例句是关闭的，这里与之保持一致；
    /// 想在预览里看例句排版，把第一条义项的 examples 补回去即可。
    /// </summary>
    public IReadOnlyList<SenseDisplayViewModel> Senses { get; } =
    [
        new("v.", [new DefinitionDisplayViewModel("漂浮；浮动", null)], []),
        new("n.", [new DefinitionDisplayViewModel("浮子；漂浮物", null)], [])
    ];

    public IReadOnlyList<RatingOptionViewModel> Ratings { get; } =
        [.. RatingScheme.Default.All.Select(rating => new RatingOptionViewModel(rating))];

    public double Progress { get; init; } = 0.42;

    public int RemainingSeconds { get; init; } = 4;

    public string PauseNotice { get; init; } = string.Empty;

    public bool IsHoverPaused { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public bool HasError { get; init; }

    public RelayCommand RateCommand { get; } = new(_ => { });
}
