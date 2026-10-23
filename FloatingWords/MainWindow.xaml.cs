using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using FloatingWords.Application.Settings;
using FloatingWords.Design;
using FloatingWords.Reading;
using FloatingWords.Settings;
using FloatingWords.Visuals;

namespace FloatingWords;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "MainViewModel 在 OnWindowClosed 中释放；WPF 管理窗口生命周期，对外暴露 IDisposable 会诱导误用（Dispose 并不会关窗）。")]
public partial class MainWindow : Window
{
    private readonly AppHost _host;
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _placementTimer;
    private bool _applyingPlacement;
    private bool _closed;

    /// <summary>
    /// XAML 设计器专用。设计器只能用公共无参构造函数创建根元素，
    /// 否则 <c>d:DataContext</c> 不生效，预览里所有绑定都会失效
    /// （Visibility 绑定失败后保持 Visible，几个页面会叠在一起）。
    /// 运行时永远走 <see cref="MainWindow(AppHost)"/>。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        InitializeNoiseTexture();

        // 设计器里没有 AppHost，只能用默认样式表撑起字号/字体，预览才和运行时一致。
        StyleResourceApplicator.Apply(Resources, SettingsDefaults.CreateStyleMap());

        DataContext = new MainWindowDesignData();

        _host = null!;
        _viewModel = null!;
        _placementTimer = null!;
    }

    public MainWindow(AppHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));

        InitializeComponent();
        InitializeNoiseTexture();

        _viewModel = new MainViewModel(
            host.VocabularyLists,
            host.Settings,
            host.Sessions,
            host.Logger);

        DataContext = _viewModel;

        ApplyStyleResources();
        ApplySavedPlacement();
        BuildContextMenu();

        _placementTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _placementTimer.Tick += OnPlacementTimerTick;

        _host.Settings.Changed += OnSettingsChanged;
        LocationChanged += OnWindowLocationChanged;
        SizeChanged += OnWindowSizeChanged;
        Closed += OnWindowClosed;
    }

    private void InitializeNoiseTexture() => CardChrome.ApplyNoise(NoiseLayer);

    private void CardClip_SizeChanged(object sender, SizeChangedEventArgs e) =>
        CardChrome.ApplyClip(CardClip, e.NewSize);

    private void ApplyStyleResources() =>
        StyleResourceApplicator.Apply(Resources, _host.Settings.Current.StyleMap);

    private void ApplySavedPlacement()
    {
        _applyingPlacement = true;

        try
        {
            WindowPlacement saved = _host.Settings.Current.Window;
            double width = double.IsFinite(saved.Width)
                ? Math.Max(MinWidth, saved.Width)
                : 508;
            double height = double.IsFinite(saved.Height)
                ? Math.Max(MinHeight, saved.Height)
                : 388;

            Width = width;
            Height = height;

            double left = double.IsFinite(saved.Left) ? saved.Left : double.NaN;
            double top = double.IsFinite(saved.Top) ? saved.Top : double.NaN;

            if (!IsPlacementVisible(left, top, width, height))
            {
                Rect workArea = SystemParameters.WorkArea;
                left = workArea.Left + Math.Max(0, (workArea.Width - width) / 2);
                top = workArea.Top + Math.Max(0, (workArea.Height - height) / 2);
            }

            Left = left;
            Top = top;
        }
        finally
        {
            _applyingPlacement = false;
        }
    }

    private static bool IsPlacementVisible(double left, double top, double width, double height)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top))
        {
            return false;
        }

        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        double virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        double right = left + width;
        double bottom = top + height;

        return right > virtualLeft + 40 &&
               left < virtualRight - 40 &&
               bottom > virtualTop + 40 &&
               top < virtualBottom - 40;
    }

    private void OnWindowLocationChanged(object? sender, EventArgs e) =>
        SchedulePlacementSave();

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e) =>
        SchedulePlacementSave();

    private void SchedulePlacementSave()
    {
        if (_applyingPlacement || _closed)
        {
            return;
        }

        _placementTimer.Stop();
        _placementTimer.Start();
    }

    private void OnPlacementTimerTick(object? sender, EventArgs e)
    {
        _placementTimer.Stop();
        SavePlacement();
    }

    private void SavePlacement()
    {
        if (_applyingPlacement || _closed || !double.IsFinite(Left) || !double.IsFinite(Top))
        {
            return;
        }

        try
        {
            _host.Settings.Update(settings => settings.Window = new WindowPlacement
            {
                Left = Left,
                Top = Top,
                Width = Width,
                Height = Height
            });
        }
        catch (Exception exception)
        {
            _host.Logger.LogError("保存窗口位置失败。", exception);
        }
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenu();

        var endItem = new MenuItem { Header = "结束本次学习" };
        endItem.Click += (_, _) => _viewModel.EndSession();

        var settingsItem = new MenuItem { Header = "设置" };
        settingsItem.Click += (_, _) => OpenSettings(SettingsSection.Reading);

        var importItem = new MenuItem { Header = "导入词书" };
        importItem.Click += (_, _) => OpenSettings(SettingsSection.Import);

        var folderItem = new MenuItem { Header = "打开数据目录" };
        folderItem.Click += (_, _) => OpenDataFolder();

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => Close();

        menu.Items.Add(endItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(importItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(folderItem);
        menu.Items.Add(exitItem);

        menu.Opened += (_, _) => endItem.IsEnabled = _viewModel.IsReading;

        ContextMenu = menu;
    }

    private void OpenDataFolder()
    {
        try
        {
            _host.DataDirectory.OpenRootFolder();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                "打开数据目录失败：" + exception.Message,
                "词浮",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenSettings(SettingsSection section)
    {
        _viewModel.PauseReadingForSettings();

        try
        {
            var window = new SettingsWindow(_host, section)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception exception)
        {
            _host.Logger.LogError("打开设置窗口失败。", exception);
            MessageBox.Show(
                this,
                "打开设置窗口失败：" + exception.Message,
                "词浮",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _viewModel.ResumeReadingFromSettings();

            if (_viewModel.Reading is not null)
            {
                _viewModel.Reading.HoverChanged(IsMouseOver);
            }
        }
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Changes.HasFlag(SettingsChangeKind.Style))
        {
            ApplyStyleResources();
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.StartSession() && _viewModel.Reading is not null)
        {
            _viewModel.Reading.HoverChanged(IsMouseOver);
        }
    }

    private void EndButton_Click(object sender, RoutedEventArgs e) => _viewModel.EndSession();

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.StartSession() && _viewModel.Reading is not null)
        {
            _viewModel.Reading.HoverChanged(IsMouseOver);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        OpenSettings(SettingsSection.Reading);

    private void ImportButton_Click(object sender, RoutedEventArgs e) =>
        OpenSettings(SettingsSection.Import);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseEnter(object sender, MouseEventArgs e) =>
        _viewModel?.Reading?.HoverChanged(true);

    private void Window_MouseLeave(object sender, MouseEventArgs e) =>
        _viewModel?.Reading?.HoverChanged(false);

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (IsInteractiveSource(e.OriginalSource as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static bool IsInteractiveSource(DependencyObject? source)
    {
        for (DependencyObject? current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase or ScrollBar or Thumb or TextBoxBase or ComboBox)
            {
                return true;
            }
        }

        return false;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _placementTimer.Stop();
        SavePlacement();
        _closed = true;

        _host.Settings.Changed -= OnSettingsChanged;
        LocationChanged -= OnWindowLocationChanged;
        SizeChanged -= OnWindowSizeChanged;
        _viewModel.Dispose();
    }
}
