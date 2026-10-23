using FloatingWords.Domain.Ratings;
using FloatingWords.Domain.Scheduling;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Domain.Tests;

public sealed class SelectionIndexTests
{
    [Fact]
    public void EmptyIndexReturnsNull()
    {
        var list = new VocabularyList("list", "空词书", "en", []);

        var index = new SelectionIndex(
            LearningItemFactory.Flatten(list),
            new Dictionary<SenseKey, string>(),
            RatingScheme.Default,
            new FakeRandomSource());

        Assert.Empty(LearningItemFactory.Flatten(list));
        Assert.Null(index.Next());
    }

    [Fact]
    public void SingleItemAvoidFallsBackToSameItem()
    {
        VocabularyList list = TestData.CreateList("one");
        IReadOnlyList<LearningItem> items = TestData.Items(list);
        var random = new FakeRandomSource();

        var index = new SelectionIndex(items, new Dictionary<SenseKey, string>(), RatingScheme.Default, random);

        LearningItem? result = index.Next(items[0].Key);

        Assert.Same(items[0], result);
        Assert.Equal(0, random.Calls);
    }

    [Theory]
    [InlineData(0, "a")]
    [InlineData(7, "a")]
    [InlineData(8, "b")]
    [InlineData(12, "c")]
    [InlineData(14, "d")]
    public void WeightedLayerSelectionSelectsExpectedLayer(long roll, string expectedSenseId)
    {
        VocabularyList list = TestData.CreateList("a", "b", "c", "d");
        IReadOnlyList<LearningItem> items = TestData.Items(list);

        var states = new Dictionary<SenseKey, string>
        {
            [TestData.Key("a")] = RatingIds.Forgot,
            [TestData.Key("b")] = RatingIds.Fuzzy,
            [TestData.Key("c")] = RatingIds.Known,
            [TestData.Key("d")] = RatingIds.Easy
        };

        var index = new SelectionIndex(items, states, RatingScheme.Default, new FakeRandomSource(roll, 0));

        LearningItem? result = index.Next();

        Assert.NotNull(result);
        Assert.Equal(expectedSenseId, result.Sense.SenseId);
    }

    [Fact]
    public void AvoidExcludesCurrentItemWhenAnotherCandidateExists()
    {
        VocabularyList list = TestData.CreateList(
            TestData.CreateEntry("w1", "alpha", "s1"),
            TestData.CreateEntry("w2", "beta", "s1"));

        IReadOnlyList<LearningItem> items = TestData.Items(list);

        var index = new SelectionIndex(items, new Dictionary<SenseKey, string>(), RatingScheme.Default, new FakeRandomSource(0, 0));

        LearningItem? result = index.Next(TestData.Key("w1", "s1"));

        Assert.NotNull(result);
        Assert.Equal("w2", result.Entry.EntryId);
    }

    [Fact]
    public void AvoidExcludesOtherSensesOfTheSameEntry()
    {
        VocabularyList list = TestData.CreateList(
            TestData.CreateEntry("w1", "alpha", "s1", "s2"),
            TestData.CreateEntry("w2", "beta", "s1"));

        IReadOnlyList<LearningItem> items = TestData.Items(list);

        var index = new SelectionIndex(items, new Dictionary<SenseKey, string>(), RatingScheme.Default, new FakeRandomSource(0, 0));

        // 避开 w1/s1 时，同词条的 w1/s2 也必须排除，否则卡片内容完全不变。
        LearningItem? result = index.Next(TestData.Key("w1", "s1"));

        Assert.NotNull(result);
        Assert.Equal("w2", result.Entry.EntryId);
    }

    [Fact]
    public void UpdateMovesItemBetweenBucketsAndKeepsCount()
    {
        VocabularyList list = TestData.CreateList("a", "b");
        IReadOnlyList<LearningItem> items = TestData.Items(list);

        var states = new Dictionary<SenseKey, string>
        {
            [TestData.Key("a")] = RatingIds.Fuzzy,
            [TestData.Key("b")] = RatingIds.Known
        };

        var index = new SelectionIndex(items, states, RatingScheme.Default, new FakeRandomSource(0, 0));

        index.Update(TestData.Key("a"), RatingIds.Known);

        Assert.Equal(2, index.Count);

        LearningItem? result = index.Next();

        Assert.NotNull(result);
        Assert.Equal("b", result.Sense.SenseId);
    }

    [Fact]
    public void UnknownRatingIsTreatedAsFuzzy()
    {
        VocabularyList list = TestData.CreateList("a");
        IReadOnlyList<LearningItem> items = TestData.Items(list);
        var states = new Dictionary<SenseKey, string>
        {
            [TestData.Key("a")] = "not-a-rating"
        };

        var index = new SelectionIndex(items, states, RatingScheme.Default, new FakeRandomSource(0, 0));

        LearningItem? result = index.Next();

        Assert.Same(items[0], result);
    }

    [Fact]
    public void DuplicateItemKeyThrows()
    {
        VocabularyList list = TestData.CreateList("a");
        LearningItem item = TestData.Items(list)[0];

        Assert.Throws<ArgumentException>(() => new SelectionIndex(
            [item, item],
            new Dictionary<SenseKey, string>(),
            RatingScheme.Default,
            new FakeRandomSource()));
    }

    [Fact]
    public void UpdateUnknownKeyThrows()
    {
        VocabularyList list = TestData.CreateList("a");
        var index = new SelectionIndex(
            TestData.Items(list),
            new Dictionary<SenseKey, string>(),
            RatingScheme.Default,
            new FakeRandomSource());

        Assert.Throws<ArgumentException>(() => index.Update(TestData.Key("missing"), RatingIds.Known));
    }
}
