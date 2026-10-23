using FloatingWords.Application.Diagnostics;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public sealed class VocabularyImportService(VocabularyReaderRegistry readers, IVocabularyRepository repository, IAppLogger? logger = null)
{
    private readonly VocabularyReaderRegistry _readers = readers ?? throw new ArgumentNullException(nameof(readers));
    private readonly IVocabularyRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAppLogger _logger = logger ?? NullAppLogger.Instance;

    /// <summary>
    /// 解析和校验来源文件，但不写数据库。
    /// </summary>
    public ImportDraft Inspect(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return InvalidDraft(sourcePath, "文件路径不能为空。");
        }

        string fileName = Path.GetFileName(sourcePath);
        long fileSize = 0;

        try
        {
            var file = new FileInfo(sourcePath);
            fileSize = file.Exists ? file.Length : 0;
        }
        catch (Exception exception)
        {
            _logger.Warn($"读取文件信息失败：{sourcePath}；{exception.Message}");
        }

        VocabularyReadResult read;

        try
        {
            read = _readers.Read(new VocabularySource(sourcePath));
        }
        catch (Exception exception)
        {
            _logger.LogError($"读取词书失败：{sourcePath}", exception);
            return InvalidDraft(sourcePath, "读取词书失败：" + exception.Message, fileSize);
        }

        var issues = read.Issues.ToList();

        if (read.List is not null)
        {
            issues.AddRange(VocabularyListRules.Validate(read.List).Select(message => new ImportIssue("$", message)));
        }

        bool isValid = read.List is not null && issues.Count == 0;
        bool conflicts = read.List is not null && _repository.Exists(read.List.ListId);

        var preview = new ImportPreview(
            sourcePath,
            fileName,
            fileSize,
            read.List?.ListId,
            read.List?.ListName,
            read.List?.Language,
            read.List?.Entries.Count ?? 0,
            read.List?.Entries.Sum(entry => entry.Senses.Count) ?? 0,
            isValid,
            conflicts,
            issues);

        var source = new VocabularySourceInfo(read.SourceFormat, read.SourceName ?? fileName);

        return new ImportDraft(preview, isValid ? read.List : null, source);
    }

    /// <summary>
    /// 将已经通过预览的内容写入数据库。失败时事务回滚。
    /// </summary>
    public ImportResult Import(ImportDraft draft, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!draft.Preview.IsValid || draft.List is null)
        {
            return new ImportResult(ImportStatus.Invalid, "词书内容未通过校验。");
        }

        if (draft.Preview.Conflicts && !overwrite)
        {
            return new ImportResult(ImportStatus.Conflict,
                $"词书 {draft.Preview.ListId} 已存在，需要确认覆盖。",
                draft.Preview.ListId);
        }

        try
        {
            _repository.Save(draft.List, draft.Source, overwrite);
            _logger.Info($"已导入词书：{draft.List.ListId}（{draft.List.ListName}）。");

            return new ImportResult(ImportStatus.Success,
                $"已导入「{draft.List.ListName}」。",
                draft.List.ListId);
        }
        catch (VocabularyConflictException)
        {
            return new ImportResult(
                ImportStatus.Conflict,
                $"词书 {draft.Preview.ListId} 已存在，需要确认覆盖。",
                draft.Preview.ListId);
        }
        catch (Exception exception)
        {
            _logger.LogError($"导入词书失败：{draft.Preview.ListId}", exception);

            return new ImportResult(
                ImportStatus.Error,
                "导入失败：" + exception.Message,
                draft.Preview.ListId);
        }
    }

    private static ImportDraft InvalidDraft(string sourcePath, string message, long fileSize = 0)
    {
        var preview = new ImportPreview(
            sourcePath,
            Path.GetFileName(sourcePath),
            fileSize,
            null,
            null,
            null,
            0,
            0,
            false,
            false,
            [new ImportIssue("$", message)]);

        return new ImportDraft(preview, null, new VocabularySourceInfo(
            Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant(),
            Path.GetFileName(sourcePath)));
    }
}
