using FloatingWords.Application.Settings;
using FloatingWords.Mvvm;

namespace FloatingWords.Settings;

public sealed class ReadingSectionViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private bool _syncing;

    public ReadingSectionViewModel(SettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Reload();
    }

    public bool IsFlashMode
    {
        get => _settings.Current.ReadingMode == ReadingMode.Flash;
        set
        {
            if (_syncing || !value)
            {
                return;
            }

            Save(settings => settings.ReadingMode = ReadingMode.Flash);
        }
    }

    public bool IsClickMode
    {
        get => _settings.Current.ReadingMode == ReadingMode.Click;
        set
        {
            if (_syncing || !value)
            {
                return;
            }

            Save(settings => settings.ReadingMode = ReadingMode.Click);
        }
    }

    public double IntervalSeconds
    {
        get => _settings.Current.RotationIntervalSeconds;
        set
        {
            if (_syncing)
            {
                return;
            }

            int interval = (int)Math.Round(value);
            interval = Math.Clamp(interval, 1, 60);

            if (interval == _settings.Current.RotationIntervalSeconds)
            {
                return;
            }

            Save(settings => settings.RotationIntervalSeconds = interval);
        }
    }

    public string IntervalText => $"每 {_settings.Current.RotationIntervalSeconds} 秒";

    public bool PauseOnHover
    {
        get => _settings.Current.PauseOnHover;
        set
        {
            if (_syncing || value == _settings.Current.PauseOnHover)
            {
                return;
            }

            Save(settings => settings.PauseOnHover = value);
        }
    }

    public bool ShowExamples
    {
        get => _settings.Current.ShowExamples;
        set
        {
            if (_syncing || value == _settings.Current.ShowExamples)
            {
                return;
            }

            Save(settings => settings.ShowExamples = value);
        }
    }

    public string Summary =>
        $"当前为 {(IsFlashMode ? "Flash 定时闪卡" : "点击优先")} · 每 {_settings.Current.RotationIntervalSeconds} 秒" +
        (IsFlashMode && PauseOnHover ? " · 悬停暂停已开启" : string.Empty) +
        (ShowExamples ? " · 显示例句" : string.Empty);

    public string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private void Save(Action<AppSettings> mutate)
    {
        try
        {
            _settings.Update(mutate);
            ErrorMessage = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = "保存失败：" + exception.Message;
        }

        Reload();
    }

    private void Reload()
    {
        _syncing = true;

        OnPropertyChanged(nameof(IsFlashMode));
        OnPropertyChanged(nameof(IsClickMode));
        OnPropertyChanged(nameof(IntervalSeconds));
        OnPropertyChanged(nameof(IntervalText));
        OnPropertyChanged(nameof(PauseOnHover));
        OnPropertyChanged(nameof(ShowExamples));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));

        _syncing = false;
    }
}
