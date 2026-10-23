using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows;
using FloatingWords.Application.Settings;
using FloatingWords.Design;
using FloatingWords.Dialogs;
using FloatingWords.Settings;
using FloatingWords.Visuals;

namespace FloatingWords;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "SettingsWindowViewModel 在 OnClosed 中释放；WPF 管理窗口生命周期，对外暴露 IDisposable 会诱导误用（Dispose 并不会关窗）。")]
public partial class SettingsWindow : Window
{
    private readonly AppHost _host;
    private readonly SettingsWindowViewModel _viewModel;

    /// <summary>
    /// XAML 设计器专用。设计器只能用公共无参构造函数创建根元素，
    /// 否则 <c>d:DataContext</c> 不生效，四个分区的 Visibility 绑定会全部失败并叠在一起。
    /// 运行时永远走 <see cref="SettingsWindow(AppHost, SettingsSection)"/>。
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
        CardChrome.ApplyNoise(NoiseLayer);

        DataContext = new SettingsWindowDesignData();

        _host = null!;
        _viewModel = null!;
    }

    public SettingsWindow(AppHost host, SettingsSection initialSection)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));

        InitializeComponent();
        CardChrome.ApplyNoise(NoiseLayer);

        _viewModel = new SettingsWindowViewModel(host, new WpfDialogService(this));
        DataContext = _viewModel;
        _viewModel.OpenSection(initialSection);

        ApplyStyleResources();
        _host.Settings.Changed += OnSettingsChanged;
        Closed += OnClosed;
    }

    private void CardClip_SizeChanged(object sender, SizeChangedEventArgs e) =>
        CardChrome.ApplyClip(CardClip, e.NewSize);

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Changes.HasFlag(SettingsChangeKind.Style))
        {
            ApplyStyleResources();
        }
    }

    private void ApplyStyleResources() =>
        StyleResourceApplicator.Apply(Resources, _host.Settings.Current.StyleMap);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _host.Settings.Changed -= OnSettingsChanged;
        _viewModel.Dispose();
    }
}
