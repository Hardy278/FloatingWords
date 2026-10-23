using FloatingWords.Application.Learning;
using FloatingWords.Application.Settings;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Ratings;
using FloatingWords.Domain.Vocabulary;
using FloatingWords.Mvvm;

namespace FloatingWords.Reading;

/// <inheritdoc />
public sealed class ReadingSessionViewModel : ObservableObject, IDisposable
{
    /// <summary>卡片高度固定，内容必须可预测：每个义项最多两条释义，例句只取第一条义项的一条。</summary>
    private const int MaxDefinitionsPerSense = 2;

    private const int MaxExamples = 1;

    private readonly RotationService _session;
    private readonly SettingsService _settings;
    private readonly CountdownController _countdown = new();

    private IAdvancePolicy _policy;
    private ReadingMode _appliedMode;
    private int _appliedInterval;
    private bool _appliedShowExamples;
    private LearningItem? _currentItem;
    private IReadOnlyList<LabelChipViewModel> _labels = [];
    private IReadOnlyList<SenseDisplayViewModel> _senses = [];
    private string _headword = string.Empty;
    private string _phoneticUk = string.Empty;
    private string _phoneticUs = string.Empty;
    private bool _showUk;
    private bool _showUs;
    private double _progress;
    private int _remainingSeconds;
    private string? _errorMessage;
    private bool _isHoverPaused;
    private bool _pointerInside;
    private bool _ratingBusy;
    private bool _advancing;
    private bool _disposed;
    private DateTime _ratingLockUntil = DateTime.MinValue;

    public ReadingSessionViewModel(RotationService session, SettingsService settings)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _appliedMode = settings.Current.ReadingMode;
        _appliedInterval = settings.Current.RotationIntervalSeconds;
        _appliedShowExamples = settings.Current.ShowExamples;
        _policy = AdvancePolicies.For(_appliedMode);

        Ratings = RatingScheme.Default.All
            .Select(rating => new RatingOptionViewModel(rating))
            .ToArray();

        RateCommand = new RelayCommand(parameter =>
        {
            if (parameter is string ratingId)
            {
                Rate(ratingId);
            }
        });

        _countdown.Tick += OnCountdownTick;
        _countdown.Elapsed += OnCountdownElapsed;

        ApplyCurrentItem(_session.Current);
        UpdateProgress();
    }

    public LearningItem? CurrentItem => _currentItem;

    public IReadOnlyList<LabelChipViewModel> Labels
    {
        get => _labels;
        private set
        {
            _labels = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<SenseDisplayViewModel> Senses
    {
        get => _senses;
        private set
        {
            _senses = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<RatingOptionViewModel> Ratings { get; }

    public RelayCommand RateCommand { get; }

    public string Headword
    {
        get => _headword;
        private set => SetProperty(ref _headword, value);
    }

    public string PhoneticUk
    {
        get => _phoneticUk;
        private set => SetProperty(ref _phoneticUk, value);
    }

    public string PhoneticUs
    {
        get => _phoneticUs;
        private set => SetProperty(ref _phoneticUs, value);
    }

    public bool ShowUk
    {
        get => _showUk;
        private set => SetProperty(ref _showUk, value);
    }

    public bool ShowUs
    {
        get => _showUs;
        private set => SetProperty(ref _showUs, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        private set => SetProperty(ref _remainingSeconds, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsHoverPaused
    {
        get => _isHoverPaused;
        private set
        {
            if (SetProperty(ref _isHoverPaused, value))
            {
                OnPropertyChanged(nameof(PauseNotice));
            }
        }
    }

    public string PauseNotice => IsHoverPaused ? "悬停暂停中" : string.Empty;

    public void Start()
    {
        ThrowIfDisposed();
        _countdown.Start(TimeSpan.FromSeconds(_settings.Current.RotationIntervalSeconds));
        UpdateProgress();
    }

    public void Rate(string ratingId)
    {
        if (_disposed || _ratingBusy || _session.Current is null)
        {
            return;
        }

        if (_policy.AdvanceOnRating && DateTime.UtcNow < _ratingLockUntil)
        {
            return;
        }

        _ratingBusy = true;

        try
        {
            Rating rating = _session.Rate(ratingId);
            ClearError();
            _countdown.Resume(PauseReason.PersistenceError);

            foreach (RatingOptionViewModel option in Ratings)
            {
                option.IsSelected = string.Equals(option.Id, rating.Id, StringComparison.Ordinal);
            }

            if (_policy.AdvanceOnRating)
            {
                _ratingLockUntil = DateTime.UtcNow.AddMilliseconds(280);
                AdvanceInternal();
            }
        }
        catch (RatingPersistenceException exception)
        {
            ErrorMessage = exception.Message;
            _countdown.Pause(PauseReason.PersistenceError);
        }
        catch (Exception exception)
        {
            ErrorMessage = "评级保存失败：" + exception.Message;
            _countdown.Pause(PauseReason.PersistenceError);
        }
        finally
        {
            _ratingBusy = false;
        }
    }

    public void HoverChanged(bool inside)
    {
        if (_disposed)
        {
            return;
        }

        _pointerInside = inside;

        if (!_policy.SupportsHoverPause || !_settings.Current.PauseOnHover)
        {
            _countdown.Resume(PauseReason.Hover);
            UpdateHoverState();
            return;
        }

        if (inside)
        {
            _countdown.Pause(PauseReason.Hover);
        }
        else
        {
            _countdown.Resume(PauseReason.Hover);
        }

        UpdateHoverState();
    }

    public void PauseForSettings() => _countdown.Pause(PauseReason.Settings);

    public void ResumeFromSettings()
    {
        _countdown.Resume(PauseReason.Settings);
        UpdateHoverState();
    }

    public void ApplySettings()
    {
        if (_disposed)
        {
            return;
        }

        _policy = AdvancePolicies.For(_settings.Current.ReadingMode);

        ReadingMode currentMode = _settings.Current.ReadingMode;
        int currentInterval = _settings.Current.RotationIntervalSeconds;

        if (currentMode != _appliedMode || currentInterval != _appliedInterval)
        {
            _countdown.Reset(TimeSpan.FromSeconds(currentInterval));
        }

        _appliedMode = currentMode;
        _appliedInterval = currentInterval;

        if (_settings.Current.ShowExamples != _appliedShowExamples)
        {
            _appliedShowExamples = _settings.Current.ShowExamples;

            if (_currentItem is not null)
            {
                ApplySenses(_currentItem);
            }
        }

        if (!_policy.SupportsHoverPause || !_settings.Current.PauseOnHover)
        {
            _countdown.Resume(PauseReason.Hover);
        }
        else if (_pointerInside)
        {
            _countdown.Pause(PauseReason.Hover);
        }
        else
        {
            _countdown.Resume(PauseReason.Hover);
        }

        UpdateHoverState();
        UpdateProgress();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _countdown.Tick -= OnCountdownTick;
        _countdown.Elapsed -= OnCountdownElapsed;
        _countdown.Dispose();
    }

    private void AdvanceInternal()
    {
        if (_disposed || _advancing)
        {
            return;
        }

        _advancing = true;

        try
        {
            LearningItem? next = _session.Advance();

            if (next is null)
            {
                ErrorMessage = "当前词书没有可显示的义项。";
                return;
            }

            ClearError();
            ApplyCurrentItem(next);
            _countdown.Reset(TimeSpan.FromSeconds(_settings.Current.RotationIntervalSeconds));
            UpdateProgress();
        }
        finally
        {
            _advancing = false;
        }
    }

    private void ApplyCurrentItem(LearningItem? item)
    {
        _currentItem = item;

        if (item is null)
        {
            Headword = string.Empty;
            Labels = [];
            Senses = [];
            ShowUk = false;
            ShowUs = false;
            OnPropertyChanged(nameof(CurrentItem));
            return;
        }

        Headword = item.Entry.Headword;

        string uk = item.Entry.Phonetic?.Uk ?? string.Empty;
        string us = item.Entry.Phonetic?.Us ?? string.Empty;

        PhoneticUk = uk;
        PhoneticUs = us;
        ShowUk = !string.IsNullOrWhiteSpace(uk);
        ShowUs = !string.IsNullOrWhiteSpace(us) && !string.Equals(uk, us, StringComparison.Ordinal);

        LabelChipViewModel[] labels = item.Entry.Senses
            .SelectMany(sense => sense.Labels)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.Ordinal)
            .Select(label => new LabelChipViewModel(label))
            .ToArray();

        Labels = labels;
        ApplySenses(item);

        foreach (RatingOptionViewModel option in Ratings)
        {
            option.IsSelected = false;
        }

        OnPropertyChanged(nameof(CurrentItem));
        UpdateProgress();
    }

    private void ApplySenses(LearningItem item)
    {
        bool showExamples = _settings.Current.ShowExamples;

        Senses = item.Entry.Senses
            .Select((sense, index) => BuildSense(sense, includeExamples: showExamples && index == 0))
            .ToArray();
    }

    private static SenseDisplayViewModel BuildSense(VocabularySense sense, bool includeExamples)
    {
        DefinitionDisplayViewModel[] definitions = sense.Definitions
            .Take(MaxDefinitionsPerSense)
            .Select(definition =>
            {
                string primary = string.IsNullOrWhiteSpace(definition.Translation)
                    ? definition.Text ?? string.Empty
                    : definition.Translation;
                string? secondary = !string.IsNullOrWhiteSpace(definition.Translation)
                    ? definition.Text
                    : null;

                return new DefinitionDisplayViewModel(primary, secondary);
            })
            .ToArray();

        ExampleDisplayViewModel[] examples = includeExamples
            ? sense.Examples
                .Take(MaxExamples)
                .Select(example =>
                {
                    string primary = string.IsNullOrWhiteSpace(example.Translation)
                        ? example.Text ?? string.Empty
                        : example.Translation;
                    string? secondary = !string.IsNullOrWhiteSpace(example.Translation)
                        ? example.Text
                        : null;

                    return new ExampleDisplayViewModel(primary, secondary);
                })
                .ToArray()
            : [];

        return new SenseDisplayViewModel(
            sense.PartOfSpeech ?? string.Empty,
            definitions,
            examples);
    }

    private void OnCountdownElapsed(object? sender, EventArgs e)
    {
        if (!_disposed && _policy.AutoAdvance)
        {
            AdvanceInternal();
        }
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        UpdateProgress();
        UpdateHoverState();
    }

    private void UpdateProgress()
    {
        Progress = _countdown.Progress;
        RemainingSeconds = _countdown.RemainingSeconds;
    }

    private void UpdateHoverState() =>
        IsHoverPaused = _countdown.PauseReasons.HasFlag(PauseReason.Hover);

    private void ClearError()
    {
        if (ErrorMessage is not null)
        {
            ErrorMessage = null;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
