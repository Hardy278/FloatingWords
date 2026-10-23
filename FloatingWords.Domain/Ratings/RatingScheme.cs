namespace FloatingWords.Domain.Ratings;

/// <summary>
/// 固定四档评级。权重表达曝光优先级，不参与任何记忆保持率计算。
/// </summary>
public sealed class RatingScheme
{
    public static RatingScheme Default { get; } = new([
        new Rating(RatingIds.Forgot, "忘了", 8, "#F0788A"),
        new Rating(RatingIds.Fuzzy, "有点印象", 4, "#E8C56B"),
        new Rating(RatingIds.Known, "会了", 2, "#75D6AE"),
        new Rating(RatingIds.Easy, "很简单", 1, "#79AEF2")
    ]);

    private readonly Dictionary<string, Rating> _byId;

    public RatingScheme(IEnumerable<Rating> ratings)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        Rating[] all = [.. ratings];

        if (all.Length == 0)
        {
            throw new ArgumentException("评级方案不能为空。", nameof(ratings));
        }

        _byId = new Dictionary<string, Rating>(StringComparer.Ordinal);

        foreach (Rating rating in all)
        {
            if (string.IsNullOrWhiteSpace(rating.Id))
            {
                throw new ArgumentException("评级 id 不能为空。", nameof(ratings));
            }

            if (rating.Weight <= 0)
            {
                throw new ArgumentException($"评级 {rating.Id} 的权重必须大于 0。", nameof(ratings));
            }

            if (!_byId.TryAdd(rating.Id, rating))
            {
                throw new ArgumentException($"评级 id 重复：{rating.Id}。", nameof(ratings));
            }
        }

        All = all;
        Fallback = _byId.TryGetValue(RatingIds.Fuzzy, out Rating? fuzzy) ? fuzzy : all[0];
    }

    public IReadOnlyList<Rating> All { get; }

    public Rating Fallback { get; }

    public Rating Normalize(string? ratingId)
    {
        if (!string.IsNullOrWhiteSpace(ratingId) &&
            _byId.TryGetValue(ratingId, out Rating? rating))
        {
            return rating;
        }

        return Fallback;
    }

    public bool TryGet(string? ratingId, out Rating rating)
    {
        if (!string.IsNullOrWhiteSpace(ratingId) &&
            _byId.TryGetValue(ratingId, out Rating? found))
        {
            rating = found;
            return true;
        }

        rating = Fallback;
        return false;
    }
}
