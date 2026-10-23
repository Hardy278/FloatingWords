namespace FloatingWords.Domain.Vocabulary;

public sealed record VocabularySense(string SenseId, string? PartOfSpeech, IReadOnlyList<string> Labels,
    IReadOnlyList<Definition> Definitions, IReadOnlyList<Example> Examples);
