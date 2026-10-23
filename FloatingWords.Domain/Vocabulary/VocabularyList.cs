namespace FloatingWords.Domain.Vocabulary;

public sealed record VocabularyList(string ListId, string ListName, string Language, IReadOnlyList<VocabularyEntry> Entries);
