using System.Text;
using System.Text.Json;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Infrastructure.Vocabulary;

public sealed class JsonVocabularyReader : IVocabularyReader
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public bool CanRead(VocabularySource? source)
        => source is not null && string.Equals(Path.GetExtension(source.FilePath), ".json", StringComparison.OrdinalIgnoreCase);

    public VocabularyReadResult Read(VocabularySource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        string fileName = Path.GetFileName(source.FilePath);
        var issues = new List<ImportIssue>();

        try
        {
            if (!File.Exists(source.FilePath))
            {
                issues.Add(new ImportIssue("$", "文件不存在。"));
                return Invalid(issues, fileName);
            }

            var fileInfo = new FileInfo(source.FilePath);

            if (fileInfo.Length > MaxFileSizeBytes)
            {
                issues.Add(new ImportIssue("$", "文件超过 20 MB，拒绝导入。"));
                return Invalid(issues, fileName);
            }

            string json = File.ReadAllText(source.FilePath, new UTF8Encoding(false, true));

            JsonVocabularyDocument? document = JsonSerializer.Deserialize<JsonVocabularyDocument>(json, JsonOptions);

            if (document is null)
            {
                issues.Add(new ImportIssue("$", "JSON 内容为空。"));
                return Invalid(issues, fileName);
            }

            VocabularyList? list = Map(document, issues);

            return list is null || issues.Count > 0 ? Invalid(issues, fileName) : new VocabularyReadResult(list, [], "json", fileName);
        }
        catch (JsonException exception)
        {
            int? line = exception.LineNumber is { } number ? (int)Math.Min(number, int.MaxValue) : null;

            issues.Add(new ImportIssue(string.IsNullOrWhiteSpace(exception.Path) ? "$" : exception.Path!,
                "JSON 解析失败：" + exception.Message, line));
        }
        catch (DecoderFallbackException)
        {
            issues.Add(new ImportIssue("$", "文件不是有效的 UTF-8 编码。"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            issues.Add(new ImportIssue("$", "读取文件失败：" + exception.Message));
        }

        return Invalid(issues, fileName);
    }

    private static VocabularyList? Map(JsonVocabularyDocument document, List<ImportIssue> issues)
    {
        if (document.SchemaVersion is < 1 or > 1)
        {
            issues.Add(new ImportIssue("schemaVersion", $"不支持词书 schemaVersion：{document.SchemaVersion}。"));
        }

        if (string.IsNullOrWhiteSpace(document.ListId))
        {
            issues.Add(new ImportIssue("listId", "listId 不能为空。"));
        }
        else if (document.ListId.Length > 80 || document.ListId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-')))
        {
            issues.Add(new ImportIssue("listId", "listId 只能包含字母、数字、下划线和连字符，长度 1–80。"));
        }

        if (string.IsNullOrWhiteSpace(document.ListName))
        {
            issues.Add(new ImportIssue("listName", "listName 不能为空。"));
        }

        if (document.Entries is null || document.Entries.Count == 0)
        {
            issues.Add(new ImportIssue("entries", "词书至少需要一个词条。"));
            return null;
        }

        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<VocabularyEntry>(document.Entries.Count);

        for (int entryIndex = 0; entryIndex < document.Entries.Count; entryIndex++)
        {
            JsonEntry? entry = document.Entries[entryIndex];
            string entryPath = $"entries[{entryIndex}]";

            if (string.IsNullOrWhiteSpace(entry.EntryId))
            {
                issues.Add(new ImportIssue($"{entryPath}.entryId", "entryId 不能为空。"));
            }
            else if (!entryIds.Add(entry.EntryId))
            {
                issues.Add(new ImportIssue($"{entryPath}.entryId", $"entryId 重复：{entry.EntryId}。"));
            }

            if (string.IsNullOrWhiteSpace(entry.Headword))
            {
                issues.Add(new ImportIssue($"{entryPath}.headword", "headword 不能为空。"));
            }

            if (entry.Senses is null || entry.Senses.Count == 0)
            {
                issues.Add(new ImportIssue($"{entryPath}.senses", "词条至少需要一个义项。"));
                continue;
            }

            var senseIds = new HashSet<string>(StringComparer.Ordinal);
            var senses = new List<VocabularySense>(entry.Senses.Count);

            for (int senseIndex = 0; senseIndex < entry.Senses.Count; senseIndex++)
            {
                JsonSense? sense = entry.Senses[senseIndex];
                string sensePath = $"{entryPath}.senses[{senseIndex}]";

                if (string.IsNullOrWhiteSpace(sense.SenseId))
                {
                    issues.Add(new ImportIssue($"{sensePath}.senseId", "senseId 不能为空。"));
                }
                else if (!senseIds.Add(sense.SenseId))
                {
                    issues.Add(new ImportIssue($"{sensePath}.senseId", $"同一词条内 senseId 重复：{sense.SenseId}。"));
                }

                if (sense.Definitions is null || sense.Definitions.Count == 0)
                {
                    issues.Add(new ImportIssue($"{sensePath}.definitions", "义项至少需要一个释义。"));
                }

                var definitions = new List<Definition>(sense.Definitions?.Count ?? 0);

                if (sense.Definitions is not null)
                {
                    for (int definitionIndex = 0; definitionIndex < sense.Definitions.Count; definitionIndex++)
                    {
                        JsonDefinition? definition = sense.Definitions[definitionIndex];

                        if (string.IsNullOrWhiteSpace(definition.Text) && string.IsNullOrWhiteSpace(definition.Translation))
                        {
                            issues.Add(new ImportIssue($"{sensePath}.definitions[{definitionIndex}]", "释义的 text 和 translation 不能同时为空。"));
                        }

                        definitions.Add(new Definition(definition.DefinitionId, definition.Text, definition.Translation));
                    }
                }

                if (definitions.Count == 0 || !definitions.Any(definition => definition.HasContent))
                {
                    issues.Add(new ImportIssue($"{sensePath}.definitions", "义项至少需要一个非空释义。"));
                }

                var examples = new List<Example>(sense.Examples?.Count ?? 0);

                if (sense.Examples is not null)
                {
                    examples.AddRange(sense.Examples.Select(example => new Example(example.ExampleId, example.Text, example.Translation)));
                }

                string[] labels = sense.Labels?
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .Select(label => label.Trim())
                    .ToArray() ?? [];

                senses.Add(new VocabularySense(
                    sense.SenseId ?? string.Empty,
                    string.IsNullOrWhiteSpace(sense.Pos) ? null : sense.Pos.Trim(),
                    labels,
                    definitions,
                    examples));
            }

            Phonetic? phonetic = entry.Phonetic is null ? null : new Phonetic(entry.Phonetic.Uk, entry.Phonetic.Us);
            if (phonetic is { HasAny: false })
            {
                phonetic = null;
            }

            entries.Add(new VocabularyEntry(
                entry.EntryId ?? string.Empty,
                entry.Headword ?? string.Empty,
                phonetic,
                senses));
        }

        if (issues.Count > 0)
        {
            return null;
        }

        return new VocabularyList(
            document.ListId!,
            document.ListName!,
            string.IsNullOrWhiteSpace(document.Language) ? "en" : document.Language.Trim(),
            entries);
    }

    private static VocabularyReadResult Invalid(IReadOnlyList<ImportIssue> issues, string fileName)
        => new(null, issues, "json", fileName);
}
