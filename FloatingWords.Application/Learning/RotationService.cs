using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Ratings;
using FloatingWords.Domain.Scheduling;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Learning;

/// <summary>
/// 一次学习会话的核心。它只负责当前义项、评级落库和推进，不感知计时器或 UI。
/// </summary>
public sealed class RotationService
{
    private readonly SelectionIndex _index;
    private readonly IRatingStore _ratingStore;
    private readonly RatingScheme _ratingScheme;
    private readonly TimeProvider _clock;

    internal RotationService(SelectionIndex index, IRatingStore ratingStore, RatingScheme ratingScheme, TimeProvider clock, SenseKey? avoid)
    {
        _index = index;
        _ratingStore = ratingStore;
        _ratingScheme = ratingScheme;
        _clock = clock;

        Current = _index.Next(avoid);
    }

    public int Count => _index.Count;

    public LearningItem? Current { get; private set; }

    public LearningItem? Advance()
    {
        Current = _index.Next(Current?.Key);
        return Current;
    }

    /// <summary>
    /// 只记录评级，不推进。写库成功之后才更新内存索引。
    /// </summary>
    public Rating Rate(string ratingId)
    {
        if (Current is null)
        {
            throw new InvalidOperationException("当前没有可评级的义项。");
        }
        Rating rating = _ratingScheme.Normalize(ratingId);

        try
        {
            _ratingStore.Save(Current.Key, rating.Id, _clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not RatingPersistenceException)
        {
            throw new RatingPersistenceException("评级保存失败。", exception);
        }
        _index.Update(Current.Key, rating.Id);
        return rating;
    }
}
