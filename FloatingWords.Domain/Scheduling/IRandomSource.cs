namespace FloatingWords.Domain.Scheduling;

public interface IRandomSource
{
    long NextInt64(long maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    public static SystemRandomSource Shared { get; } = new();

    public long NextInt64(long maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return Random.Shared.NextInt64(maxExclusive);
    }
}
