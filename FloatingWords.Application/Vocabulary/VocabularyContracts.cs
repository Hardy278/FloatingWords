namespace FloatingWords.Application.Vocabulary;

public sealed record VocabularySource(string FilePath);

public sealed record VocabularySourceInfo(string Format, string? Name);

public sealed record VocabularyListSummary(
    string ListId,
    string ListName,
    string Language,
    int EntryCount,
    int SenseCount,
    string SourceFormat,
    string? SourceName,
    DateTimeOffset ImportedAt);

public sealed class VocabularyNotFoundException(string listId) : Exception($"词书不存在：{listId}")
{
    public string ListId { get; } = listId;
}

public sealed class VocabularyConflictException(string listId) : Exception($"词书已存在：{listId}")
{
    public string ListId { get; } = listId;
}

public sealed class VocabularyStorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
