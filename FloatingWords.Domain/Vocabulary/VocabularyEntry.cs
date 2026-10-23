namespace FloatingWords.Domain.Vocabulary;

public sealed record VocabularyEntry(string EntryId, string Headword, Phonetic? Phonetic, IReadOnlyList<VocabularySense> Senses);
