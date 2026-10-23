namespace FloatingWords.Domain.Vocabulary;

public static class LearningItemFactory
{
    public static IReadOnlyList<LearningItem> Flatten(VocabularyList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        var items = new List<LearningItem>();
        foreach (VocabularyEntry entry in list.Entries)
        {
            foreach (VocabularySense sense in entry.Senses)
            {
                items.Add(new LearningItem(list, entry, sense));
            }
        }
        return items;
    }
}
