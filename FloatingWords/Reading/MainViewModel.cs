using FloatingWords.Application.Diagnostics;
using FloatingWords.Application.Learning;
using FloatingWords.Application.Settings;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Vocabulary;
using FloatingWords.Mvvm;

namespace FloatingWords.Reading;

public enum WorkspacePage
{
    Ready,
    Reading,
    Empty,
    Error
}

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly VocabularyListManager _lists;
    private readonly SettingsService _settings;
    private readonly LearningSessionFactory _sessions;
    private readonly IAppLogger _logger;

    private ReadingSessionViewModel? _reading;
    private SenseKey? _lastShownKey;
    private string _currentListName = "暂无词书";
    private string _modeSummary = "Flash · 每 5 秒";
    private string _hoverHint = "悬停暂停 · 评级不会打断节奏";
    private string _stateMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _disposed;

    public MainViewModel(
        VocabularyListManager lists,
        SettingsService settings,
        LearningSessionFactory sessions,
        IAppLogger? logger = null)
    {
        _lists = lists ?? throw new ArgumentNullException(nameof(lists));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? NullAppLogger.Instance;

        StartCommand = new RelayCommand(() =>
        {
            StartSession();
        });
        EndCommand = new RelayCommand(EndSession);
        RetryCommand = new RelayCommand(() =>
        {
            StartSession();
        });
        ImportCommand = new RelayCommand(() => ImportRequested?.Invoke(this, EventArgs.Empty));
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty));

        _lists.ListsChanged += OnListsChanged;
        _lists.CurrentListChanged += OnCurrentListChanged;
        _settings.Changed += OnSettingsChanged;

        RefreshSummary();
    }

    public event EventHandler? SettingsRequested;

    public event EventHandler? ImportRequested;

    public WorkspacePage Page { get; private set; } = WorkspacePage.Ready;

    public bool IsReady => Page == WorkspacePage.Ready;

    public bool IsReading => Page == WorkspacePage.Reading;

    public bool IsEmpty => Page == WorkspacePage.Empty;

    public bool IsError => Page == WorkspacePage.Error;

    public bool IsState => IsEmpty || IsError;

    public ReadingSessionViewModel? Reading
    {
        get => _reading;
        private set => SetProperty(ref _reading, value);
    }

    public string CurrentListName
    {
        get => _currentListName;
        private set
        {
            if (SetProperty(ref _currentListName, value))
            {
                OnPropertyChanged(nameof(SummaryLine));
            }
        }
    }

    public string ModeSummary
    {
        get => _modeSummary;
        private set
        {
            if (SetProperty(ref _modeSummary, value))
            {
                OnPropertyChanged(nameof(SummaryLine));
            }
        }
    }

    public string SummaryLine => $"{CurrentListName} · {ModeSummary}";

    public string HoverHint
    {
        get => _hoverHint;
        private set => SetProperty(ref _hoverHint, value);
    }

    public string StateMessage
    {
        get => _stateMessage;
        private set => SetProperty(ref _stateMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool HasCurrentList => _lists.CurrentListId is not null;

    public bool HasNoCurrentList => !HasCurrentList;

    public bool HasLists => _lists.Lists.Count > 0;

    public RelayCommand StartCommand { get; }

    public RelayCommand EndCommand { get; }

    public RelayCommand RetryCommand { get; }

    public RelayCommand ImportCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public bool StartSession()
    {
        if (_disposed)
        {
            return false;
        }

        if (Reading is not null)
        {
            CaptureLastShown();
            Reading.Dispose();
            Reading = null;
        }

        try
        {
            VocabularyList? list = _lists.LoadCurrent();

            if (list is null || list.Entries.Count == 0)
            {
                SetPage(WorkspacePage.Empty, "暂时没有可显示的义项。请导入词书，或在设置中切换词书。");
                return false;
            }

            RotationService session = _sessions.Create(list, _lastShownKey);

            if (session.Count == 0)
            {
                SetPage(WorkspacePage.Empty, "暂时没有可显示的义项。请导入词书，或在设置中切换词书。");
                return false;
            }

            Reading = new ReadingSessionViewModel(session, _settings);
            SetPage(WorkspacePage.Reading);
            Reading.Start();
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError("启动学习会话失败。", exception);
            Reading?.Dispose();
            Reading = null;
            SetPage(WorkspacePage.Error, "无法读取当前词书：" + exception.Message);
            return false;
        }
    }

    public void EndSession()
    {
        if (Reading is null)
        {
            if (Page != WorkspacePage.Ready)
            {
                SetPage(WorkspacePage.Ready);
            }

            return;
        }

        CaptureLastShown();
        Reading.Dispose();
        Reading = null;
        SetPage(WorkspacePage.Ready);
        RefreshSummary();
    }

    public void ApplyReadingSettings() => Reading?.ApplySettings();

    public void PauseReadingForSettings() => Reading?.PauseForSettings();

    public void ResumeReadingFromSettings() => Reading?.ResumeFromSettings();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lists.ListsChanged -= OnListsChanged;
        _lists.CurrentListChanged -= OnCurrentListChanged;
        _settings.Changed -= OnSettingsChanged;

        Reading?.Dispose();
        Reading = null;
    }

    private void CaptureLastShown()
    {
        if (Reading?.CurrentItem is { } item)
        {
            _lastShownKey = item.Key;
        }
    }

    private void OnListsChanged(object? sender, EventArgs e) => RefreshSummary();

    private void OnCurrentListChanged(object? sender, EventArgs e)
    {
        if (Reading is not null)
        {
            CaptureLastShown();
            Reading.Dispose();
            Reading = null;
        }

        SetPage(WorkspacePage.Ready);
        RefreshSummary();
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Changes.HasFlag(SettingsChangeKind.Reading) ||
            e.Changes.HasFlag(SettingsChangeKind.CurrentBook))
        {
            RefreshSummary();
        }

        if (Reading is not null && e.Changes.HasFlag(SettingsChangeKind.Reading))
        {
            Reading.ApplySettings();
        }
    }

    private void RefreshSummary()
    {
        VocabularyListSummary? current = _lists.Lists.FirstOrDefault(list =>
            string.Equals(list.ListId, _lists.CurrentListId, StringComparison.Ordinal));

        CurrentListName = current?.ListName ?? "暂无词书";
        ModeSummary = _settings.Current.ReadingMode == ReadingMode.Flash
            ? $"Flash · 每 {_settings.Current.RotationIntervalSeconds} 秒"
            : $"点击优先 · 每 {_settings.Current.RotationIntervalSeconds} 秒";
        HoverHint = _settings.Current.PauseOnHover
            ? "悬停暂停 · 评级不会打断节奏"
            : "评级不会打断节奏";

        OnPropertyChanged(nameof(HasCurrentList));
        OnPropertyChanged(nameof(HasNoCurrentList));
        OnPropertyChanged(nameof(HasLists));
    }

    private void SetPage(WorkspacePage page, string? message = null)
    {
        Page = page;

        switch (page)
        {
            case WorkspacePage.Empty:
                StateMessage = message ?? "暂时没有可显示的义项。";
                break;
            case WorkspacePage.Error:
                StateMessage = "暂时无法显示词书内容。";
                ErrorMessage = message ?? "发生了未知错误。";
                break;
        }

        OnPropertyChanged(nameof(Page));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(IsReading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(IsState));
        OnPropertyChanged(nameof(HasCurrentList));
        OnPropertyChanged(nameof(HasNoCurrentList));
        OnPropertyChanged(nameof(HasLists));
    }
}
