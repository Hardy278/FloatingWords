namespace FloatingWords.Application.Settings;

public static class TextRoles
{
    public const string Headword = "headword";
    public const string Phonetic = "phonetic";
    public const string PartOfSpeech = "pos";
    public const string Definition = "definition";
    public const string DefinitionTranslation = "definitionTranslation";
    public const string Example = "example";
    public const string ExampleTranslation = "exampleTranslation";
    public const string Label = "label";

    public static IReadOnlyList<string> All { get; } =
    [
        Headword,
        Phonetic,
        PartOfSpeech,
        Definition,
        DefinitionTranslation,
        Example,
        ExampleTranslation,
        Label
    ];
}
