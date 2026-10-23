using FloatingWords.Application.Vocabulary;
using FloatingWords.Infrastructure.Vocabulary;

namespace FloatingWords.Infrastructure.Tests;

public sealed class JsonVocabularyReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "FloatingWordsTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ReadValidJsonReturnsList()
    {
        string path = WriteJson(
            """
            {
              "schemaVersion": 1,
              "listId": "test-list",
              "listName": "测试词书",
              "language": "en",
              "entries": [
                {
                  "entryId": "entry-1",
                  "headword": "test",
                  "phonetic": { "uk": "/test/", "us": "/test/" },
                  "senses": [
                    {
                      "senseId": "sense-1",
                      "pos": "n.",
                      "labels": ["核心"],
                      "definitions": [
                        { "text": "a test", "translation": "测试" }
                      ],
                      "examples": [
                        { "text": "This is a test.", "translation": "这是一个测试。" }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

        var reader = new JsonVocabularyReader();
        VocabularyReadResult result = reader.Read(new VocabularySource(path));

        Assert.True(result.IsValid);
        Assert.NotNull(result.List);
        Assert.Equal("test-list", result.List.ListId);
        Assert.Single(result.List.Entries);
        Assert.Single(result.List.Entries[0].Senses);
    }

    [Fact]
    public void ReadDuplicateSenseIdsReturnsIssues()
    {
        string path = WriteJson(
            """
            {
              "listId": "test-list",
              "listName": "测试词书",
              "entries": [
                {
                  "entryId": "entry-1",
                  "headword": "test",
                  "senses": [
                    { "senseId": "same", "definitions": [{ "text": "a" }] },
                    { "senseId": "same", "definitions": [{ "text": "b" }] }
                  ]
                }
              ]
            }
            """);

        var reader = new JsonVocabularyReader();
        VocabularyReadResult result = reader.Read(new VocabularySource(path));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Message.Contains("senseId 重复", StringComparison.Ordinal));
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

    private string WriteJson(string content)
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "test.json");
        File.WriteAllText(path, content);
        return path;
    }
}
