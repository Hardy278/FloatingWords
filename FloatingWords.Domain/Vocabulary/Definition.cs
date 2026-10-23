namespace FloatingWords.Domain.Vocabulary;

public sealed record Definition(string? DefinitionId, string? Text, string? Translation)
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Text) || !string.IsNullOrWhiteSpace(Translation);
}
