namespace FloatingWords.Application.Settings;

[Flags]
public enum SettingsChangeKind
{
    None = 0,
    Reading = 1,
    Style = 2,
    Window = 4,
    CurrentBook = 8
}

public sealed class SettingsChangedEventArgs(SettingsChangeKind changes) : EventArgs
{
    public SettingsChangeKind Changes { get; } = changes;
}

public sealed class SettingsPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed record SettingsLoadResult(AppSettings Settings, string? Warning);
