namespace FloatingWords.Domain.Vocabulary;

/// <summary>
/// 与文件格式无关的词书规则。JSON、Excel 和数据库写入都应该经过这些约束。
/// </summary>
public static class VocabularyListRules
{
    public static IReadOnlyList<string> Validate(VocabularyList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(list.ListId))
        {
            issues.Add("listId 不能为空。");
        }
        else if (list.ListId.Length > 80 || list.ListId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-')))
        {
            issues.Add("listId 只能包含字母、数字、下划线和连字符，长度 1–80。");
        }

        if (string.IsNullOrWhiteSpace(list.ListName))
        {
            issues.Add("listName 不能为空。");
        }

        if (list.Entries.Count == 0)
        {
            issues.Add("词书至少需要一个词条。");
            return issues;
        }

        var entryIds = new HashSet<string>(StringComparer.Ordinal);

        for (int entryIndex = 0; entryIndex < list.Entries.Count; entryIndex++)
        {
            VocabularyEntry entry = list.Entries[entryIndex];
            string entryPath = $"entries[{entryIndex}]";

            if (string.IsNullOrWhiteSpace(entry.EntryId))
            {
                issues.Add($"{entryPath}.entryId 不能为空。");
            }
            else if (!entryIds.Add(entry.EntryId))
            {
                issues.Add($"{entryPath}.entryId 重复：{entry.EntryId}。");
            }

            if (string.IsNullOrWhiteSpace(entry.Headword))
            {
                issues.Add($"{entryPath}.headword 不能为空。");
            }

            if (entry.Senses.Count == 0)
            {
                issues.Add($"{entryPath} 至少需要一个义项。");
                continue;
            }

            var senseIds = new HashSet<string>(StringComparer.Ordinal);

            for (int senseIndex = 0; senseIndex < entry.Senses.Count; senseIndex++)
            {
                VocabularySense sense = entry.Senses[senseIndex];
                string sensePath = $"{entryPath}.senses[{senseIndex}]";

                if (string.IsNullOrWhiteSpace(sense.SenseId))
                {
                    issues.Add($"{sensePath}.senseId 不能为空。");
                }
                else if (!senseIds.Add(sense.SenseId))
                {
                    issues.Add($"{sensePath}.senseId 在同一词条内重复：{sense.SenseId}。");
                }

                if (sense.Definitions.Count == 0 || !sense.Definitions.Any(definition => definition.HasContent))
                {
                    issues.Add($"{sensePath} 至少需要一个非空释义。");
                }
            }
        }

        return issues;
    }
}
