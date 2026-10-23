namespace FloatingWords.Domain.Vocabulary;

public sealed record Example(string? ExampleId, string? Text, string? Translation)
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Text) || !string.IsNullOrWhiteSpace(Translation);
}
