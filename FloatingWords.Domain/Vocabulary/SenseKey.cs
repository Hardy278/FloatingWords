namespace FloatingWords.Domain.Vocabulary;

/// <summary>
/// 一个义项的稳定身份。评级和调度都以它为准。
/// </summary>
public sealed record SenseKey(string ListId, string EntryId, string SenseId)
{
    public override string ToString() => $"{ListId}/{EntryId}/{SenseId}";
}
