namespace FloatingWords.Application.Settings;

public sealed record WindowPlacement
{
    public double Left { get; init; } = 100;

    public double Top { get; init; } = 100;

    public double Width { get; init; } = 508;

    public double Height { get; init; } = 388;
}
