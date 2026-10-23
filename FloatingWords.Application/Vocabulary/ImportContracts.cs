using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public sealed record ImportPreview(
    string SourcePath,
    string FileName,
    long FileSize,
    string? ListId,
    string? ListName,
    string? Language,
    int EntryCount,
    int SenseCount,
    bool IsValid,
    bool Conflicts,
    IReadOnlyList<ImportIssue> Issues);

public sealed class ImportDraft
{
    internal ImportDraft(ImportPreview preview, VocabularyList? list, VocabularySourceInfo source)
    {
        Preview = preview;
        List = list;
        Source = source;
    }

    public ImportPreview Preview { get; }

    internal VocabularyList? List { get; }

    internal VocabularySourceInfo Source { get; }
}

public enum ImportStatus
{
    Success,
    Conflict,
    Invalid,
    Error
}

public sealed record ImportResult(ImportStatus Status, string Message, string? ListId = null);
