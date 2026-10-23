namespace FloatingWords.Application.Settings;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public string? CurrentListId { get; set; }

    public ReadingMode ReadingMode { get; set; } = ReadingMode.Flash;

    public int RotationIntervalSeconds { get; set; } = 5;

    public bool PauseOnHover { get; set; } = true;

    public bool ShowExamples { get; set; }

    public WindowPlacement Window { get; set; } = new();

    public Dictionary<string, TextStyle> StyleMap { get; set; } = new(StringComparer.Ordinal);

    public AppSettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        CurrentListId = CurrentListId,
        ReadingMode = ReadingMode,
        RotationIntervalSeconds = RotationIntervalSeconds,
        PauseOnHover = PauseOnHover,
        ShowExamples = ShowExamples,
        Window = Window ?? new WindowPlacement(),
        StyleMap = new Dictionary<string, TextStyle>(
            StyleMap ?? new Dictionary<string, TextStyle>(StringComparer.Ordinal),
            StringComparer.Ordinal)
    };
}
