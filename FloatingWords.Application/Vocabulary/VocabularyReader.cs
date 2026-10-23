using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public sealed record ImportIssue(string Path, string Message, int? Line = null);

public sealed record VocabularyReadResult(VocabularyList? List, IReadOnlyList<ImportIssue> Issues, string SourceFormat, string? SourceName)
{
    public bool IsValid => List is not null && Issues.Count == 0;
}

public interface IVocabularyReader
{
    bool CanRead(VocabularySource source);

    VocabularyReadResult Read(VocabularySource source);
}

public sealed class VocabularyReaderRegistry
{
    private readonly IReadOnlyList<IVocabularyReader> _readers;

    public VocabularyReaderRegistry(IEnumerable<IVocabularyReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        _readers = [.. readers];
    }

    public VocabularyReadResult Read(VocabularySource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (IVocabularyReader reader in _readers)
        {
            if (reader.CanRead(source))
            {
                return reader.Read(source);
            }
        }

        string extension = Path.GetExtension(source.FilePath);

        return new VocabularyReadResult(
            null,
            [new ImportIssue("$", $"暂不支持这种词书格式：{extension}。当前仅支持 JSON。")],
            extension.TrimStart('.').ToLowerInvariant(),
            Path.GetFileName(source.FilePath));
    }
}
