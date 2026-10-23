namespace FloatingWords.Infrastructure.Vocabulary;

internal sealed class JsonVocabularyDocument
{
    public int? SchemaVersion { get; set; }

    public string? ListId { get; set; }

    public string? ListName { get; set; }

    public string? Language { get; set; }

    public List<JsonEntry>? Entries { get; set; }
}

internal sealed class JsonEntry
{
    public string? EntryId { get; set; }

    public string? Headword { get; set; }

    public JsonPhonetic? Phonetic { get; set; }

    public List<JsonSense>? Senses { get; set; }
}

internal sealed class JsonPhonetic
{
    public string? Uk { get; set; }

    public string? Us { get; set; }
}

internal sealed class JsonSense
{
    public string? SenseId { get; set; }

    public string? Pos { get; set; }

    public List<string>? Labels { get; set; }

    public List<JsonDefinition>? Definitions { get; set; }

    public List<JsonExample>? Examples { get; set; }
}

internal sealed class JsonDefinition
{
    public string? DefinitionId { get; set; }

    public string? Text { get; set; }

    public string? Translation { get; set; }
}

internal sealed class JsonExample
{
    public string? ExampleId { get; set; }

    public string? Text { get; set; }

    public string? Translation { get; set; }
}
