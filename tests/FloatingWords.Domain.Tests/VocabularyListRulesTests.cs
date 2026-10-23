using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Domain.Tests;

public sealed class VocabularyListRulesTests
{
    [Fact]
    public void ValidListHasNoIssues()
    {
        VocabularyList list = TestData.CreateList("a", "b");

        Assert.Empty(VocabularyListRules.Validate(list));
    }

    [Fact]
    public void DuplicateEntryIdIsRejected()
    {
        IReadOnlyList<Definition> definitions = [new(null, "text", null)];
        IReadOnlyList<VocabularySense> senses = [new("sense", "n.", [], definitions, [])];

        var entry = new VocabularyEntry("same", "word", null, senses);
        var list = new VocabularyList("valid-id", "词书", "en", [entry, entry]);

        Assert.Contains(VocabularyListRules.Validate(list), issue => issue.Contains("entryId 重复", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyDefinitionsAreRejected()
    {
        IReadOnlyList<Definition> definitions = [new(null, " ", null)];
        IReadOnlyList<VocabularySense> senses = [new("sense", "n.", [], definitions, [])];

        var entry = new VocabularyEntry("entry", "word", null, senses);
        var list = new VocabularyList("valid-id", "词书", "en", [entry]);

        Assert.Contains(VocabularyListRules.Validate(list), issue => issue.Contains("非空释义", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidListIdIsRejected()
    {
        var list = new VocabularyList("not valid", "词书", "en", TestData.CreateList("a").Entries);

        Assert.Contains(VocabularyListRules.Validate(list), issue => issue.Contains("listId", StringComparison.Ordinal));
    }
}
