using FloatingWords.Domain.Scheduling;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Domain.Tests;

internal static class TestData
{
    public static VocabularyList CreateList(params string[] senseIds)
    {
        IReadOnlyList<Definition> definitions = [new(null, "definition", "释义")];

        VocabularySense[] senses = [.. senseIds.Select(senseId
            => new VocabularySense(senseId, "n.", ["测试"], definitions, []))];

        var entry = new VocabularyEntry("entry", "test", null, senses);

        return new VocabularyList("list", "测试词书", "en", [entry]);
    }

    public static IReadOnlyList<LearningItem> Items(VocabularyList list) => LearningItemFactory.Flatten(list);

    public static VocabularyEntry CreateEntry(string entryId, string headword, params string[] senseIds)
    {
        IReadOnlyList<Definition> definitions = [new(null, "definition", "释义")];

        VocabularySense[] senses = [.. senseIds.Select(senseId
            => new VocabularySense(senseId, "n.", ["测试"], definitions, []))];

        return new VocabularyEntry(entryId, headword, null, senses);
    }

    public static VocabularyList CreateList(params VocabularyEntry[] entries) =>
        new("list", "测试词书", "en", [.. entries]);

    public static SenseKey Key(string senseId) => new("list", "entry", senseId);

    public static SenseKey Key(string entryId, string senseId) => new("list", entryId, senseId);
}

internal sealed class FakeRandomSource(params long[] values) : IRandomSource
{
    private readonly Queue<long> _values = new(values);

    public int Calls { get; private set; }

    public long NextInt64(long maxExclusive)
    {
        Calls++;
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("随机序列已经用完。");
        }

        long value = _values.Dequeue();
        if (value < 0 || value >= maxExclusive)
        {
            throw new InvalidOperationException($"测试随机值 {value} 不在 [0, {maxExclusive}) 内。");
        }
        return value;
    }
}
