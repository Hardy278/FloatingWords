namespace FloatingWords.Application.Settings;

public sealed record TextStyle
{
    public string FontFamily { get; init; } = "Segoe UI";

    public double FontSize { get; init; } = 14;

    public string FontWeight { get; init; } = "Normal";

    public string Color { get; init; } = "#FFFFFF";

    public bool Italic { get; init; }

    public string? BackgroundColor { get; init; }
}
