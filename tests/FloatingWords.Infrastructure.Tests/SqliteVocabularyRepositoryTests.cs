using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Vocabulary;
using FloatingWords.Infrastructure.Sqlite;

namespace FloatingWords.Infrastructure.Tests;

public sealed class SqliteVocabularyRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly SqliteVocabularyRepository _repository;
    private readonly SqliteRatingStore _ratings;

    public SqliteVocabularyRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "FloatingWordsTests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_directory);

        var database = new SqliteDatabase(Path.Combine(_directory, "test.db"));
        database.Initialize();

        _repository = new SqliteVocabularyRepository(database);
        _ratings = new SqliteRatingStore(database);
    }

    [Fact]
    public void SaveAndLoadRoundTripsContent()
    {
        VocabularyList list = CreateList("s1", "s2");

        _repository.Save(list, new VocabularySourceInfo("json", "test.json"), overwrite: false);

        IReadOnlyList<VocabularyListSummary> summaries = _repository.GetSummaries();

        Assert.Single(summaries);
        Assert.Equal("test", summaries[0].ListId);
        Assert.Equal(1, summaries[0].EntryCount);
        Assert.Equal(2, summaries[0].SenseCount);

        VocabularyList? loaded = _repository.Load("test");

        Assert.NotNull(loaded);
        Assert.Equal("测试词书", loaded.ListName);
        Assert.Equal("en", loaded.Language);
        Assert.Single(loaded.Entries);
        Assert.Equal(2, loaded.Entries[0].Senses.Count);
    }

    [Fact]
    public void DeleteCascadesRatings()
    {
        VocabularyList list = CreateList("s1");
        _repository.Save(list, new VocabularySourceInfo("json", "test.json"), overwrite: false);

        var key = new SenseKey("test", "entry", "s1");
        _ratings.Save(key, "known", DateTimeOffset.UtcNow);

        Assert.NotEmpty(_ratings.Load("test"));

        _repository.Delete("test");

        Assert.Empty(_ratings.Load("test"));
        Assert.False(_repository.Exists("test"));
    }

    [Fact]
    public void OverwritePreservesMatchingRatingAndRemovesMissingSense()
    {
        VocabularyList original = CreateList("s1", "s2");
        _repository.Save(original, new VocabularySourceInfo("json", "original.json"), overwrite: false);

        _ratings.Save(new SenseKey("test", "entry", "s1"), "known", DateTimeOffset.UtcNow);
        _ratings.Save(new SenseKey("test", "entry", "s2"), "forgot", DateTimeOffset.UtcNow);

        VocabularyList replacement = CreateList("s1");
        _repository.Save(replacement, new VocabularySourceInfo("json", "replacement.json"), overwrite: true);

        IReadOnlyDictionary<SenseKey, string> states = _ratings.Load("test");

        Assert.True(states.ContainsKey(new SenseKey("test", "entry", "s1")));
        Assert.False(states.ContainsKey(new SenseKey("test", "entry", "s2")));
        Assert.Equal("known", states[new SenseKey("test", "entry", "s1")]);
    }

    [Fact]
    public void SaveExistingWithoutOverwriteThrowsConflict()
    {
        VocabularyList list = CreateList("s1");
        _repository.Save(list, new VocabularySourceInfo("json", "test.json"), overwrite: false);

        Assert.Throws<VocabularyConflictException>(() =>
            _repository.Save(list, new VocabularySourceInfo("json", "test.json"), overwrite: false));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // 测试清理失败不改变测试结果。
        }
    }

    private static VocabularyList CreateList(params string[] senseIds)
    {
        VocabularySense[] senses = senseIds
            .Select(senseId => new VocabularySense(
                senseId,
                "n.",
                ["测试"],
                [new Definition(null, "definition", "释义")],
                []))
            .ToArray();

        var entry = new VocabularyEntry("entry", "test", null, senses);

        return new VocabularyList("test", "测试词书", "en", [entry]);
    }
}
