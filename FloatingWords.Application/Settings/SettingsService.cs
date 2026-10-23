using FloatingWords.Application.Diagnostics;

namespace FloatingWords.Application.Settings;

public sealed class SettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly IAppLogger _logger;

    public SettingsService(ISettingsRepository repository, IAppLogger? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? NullAppLogger.Instance;

        (AppSettings settings, string? warning) = _repository.Load();
        SettingsDefaults.Normalize(settings);

        Current = settings;
        LoadWarning = warning;

        if (!string.IsNullOrWhiteSpace(LoadWarning))
        {
            _logger.Warn(LoadWarning);
        }
    }

    public AppSettings Current { get; private set; }

    public string? LoadWarning { get; }

    public event EventHandler<SettingsChangedEventArgs>? Changed;

    public void Update(Action<AppSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        AppSettings candidate = Current.Clone();
        mutate(candidate);
        SettingsDefaults.Normalize(candidate);

        SettingsChangeKind changes = AppSettingsComparer.GetChanges(Current, candidate);

        if (changes == SettingsChangeKind.None)
        {
            return;
        }

        try
        {
            _repository.Save(candidate);
        }
        catch (SettingsPersistenceException exception)
        {
            _logger.LogError("保存设置失败。", exception);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError("保存设置失败。", exception);
            throw new SettingsPersistenceException("保存设置失败。", exception);
        }

        Current = candidate;
        Changed?.Invoke(this, new SettingsChangedEventArgs(changes));
    }
}
