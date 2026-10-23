namespace FloatingWords.Domain.Vocabulary;

/// <summary>
/// 调度与展示使用的最小单元：一个词条下的一个义项。
/// </summary>
public sealed class LearningItem(VocabularyList list, VocabularyEntry entry, VocabularySense sense)
{
    public SenseKey Key { get; } = new(list.ListId, entry.EntryId, sense.SenseId);

    public VocabularyList List { get; } = list;

    public VocabularyEntry Entry { get; } = entry;

    public VocabularySense Sense { get; } = sense;
}
