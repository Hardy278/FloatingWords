namespace FloatingWords.Domain.Vocabulary;

public sealed record Phonetic(string? Uk, string? Us)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(Uk) || !string.IsNullOrWhiteSpace(Us);
}
