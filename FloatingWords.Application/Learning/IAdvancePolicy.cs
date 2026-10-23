using FloatingWords.Application.Settings;

namespace FloatingWords.Application.Learning;

public interface IAdvancePolicy
{
    bool AutoAdvance { get; }

    bool AdvanceOnRating { get; }

    bool SupportsHoverPause { get; }
}

public sealed class FlashAdvancePolicy : IAdvancePolicy
{
    public bool AutoAdvance => true;

    public bool AdvanceOnRating => false;

    public bool SupportsHoverPause => true;
}

public sealed class ClickAdvancePolicy : IAdvancePolicy
{
    public bool AutoAdvance => true;

    public bool AdvanceOnRating => true;

    public bool SupportsHoverPause => false;
}

public static class AdvancePolicies
{
    public static IAdvancePolicy For(ReadingMode mode) => mode switch
    {
        ReadingMode.Flash => new FlashAdvancePolicy(),
        ReadingMode.Click => new ClickAdvancePolicy(),
        _ => new FlashAdvancePolicy()
    };
}
