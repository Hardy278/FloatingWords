using FloatingWords.Domain.Ratings;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Domain.Scheduling;

/// <summary>
/// 按评级层权重抽词。层内均匀选择，并在可能时避开当前词条（而不是单个义项）。
/// </summary>
public sealed class SelectionIndex
{
    private readonly RatingScheme _scheme;
    private readonly IRandomSource _random;
    private readonly Dictionary<string, List<LearningItem>> _buckets;
    private readonly Dictionary<SenseKey, Position> _positions;

    public SelectionIndex(IEnumerable<LearningItem> items, IReadOnlyDictionary<SenseKey, string> ratingByKey, RatingScheme scheme, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(ratingByKey);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(random);

        _scheme = scheme;
        _random = random;
        _buckets = new Dictionary<string, List<LearningItem>>(StringComparer.Ordinal);
        _positions = new Dictionary<SenseKey, Position>();

        foreach (Rating rating in scheme.All)
        {
            _buckets[rating.Id] = [];
        }

        foreach (LearningItem item in items)
        {
            if (_positions.ContainsKey(item.Key))
            {
                throw new ArgumentException($"学习项重复：{item.Key}。", nameof(items));
            }

            Rating rating = scheme.Normalize(ratingByKey.GetValueOrDefault(item.Key));

            List<LearningItem> bucket = _buckets[rating.Id];
            int index = bucket.Count;
            bucket.Add(item);
            _positions[item.Key] = new Position(rating.Id, index);
            Count++;
        }
    }

    public int Count { get; }

    public LearningItem? Next(SenseKey? avoid = null)
    {
        if (Count == 0)
        {
            return null;
        }

        // 卡片渲染的是整个词条（headword + 全部义项），所以必须按 EntryId 避开。
        // 只避开 SenseKey 时，同一词条的另一义项仍会被抽中，画面一个字都不会变。
        // 同时比较 ListId：切换词书后传入的旧 key 不应误伤新词书里同名的 entryId。

        IReadOnlyList<Rating> ratings = _scheme.All;
        int[] candidateCounts = new int[ratings.Count];
        long totalWeight = 0;

        for (int index = 0; index < ratings.Count; index++)
        {
            int candidates = CountCandidates(_buckets[ratings[index].Id], avoid);
            candidateCounts[index] = candidates;

            if (candidates > 0)
            {
                totalWeight = checked(totalWeight + (long)ratings[index].Weight * candidates);
            }
        }

        if (totalWeight <= 0)
        {
            // 整本词书只有当前这个词条时无法避开，退回任意一项，避免卡片空白。
            foreach (Rating rating in ratings)
            {
                List<LearningItem> bucket = _buckets[rating.Id];

                if (bucket.Count > 0)
                {
                    return bucket[0];
                }
            }

            return null;
        }

        long roll = _random.NextInt64(totalWeight);

        for (int index = 0; index < ratings.Count; index++)
        {
            int candidates = candidateCounts[index];

            if (candidates <= 0)
            {
                continue;
            }

            long layerWeight = (long)ratings[index].Weight * candidates;

            if (roll >= layerWeight)
            {
                roll -= layerWeight;
                continue;
            }

            int target = (int)_random.NextInt64(candidates);

            foreach (LearningItem item in _buckets[ratings[index].Id])
            {
                if (IsSameEntry(item, avoid))
                {
                    continue;
                }

                if (target-- == 0)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private static int CountCandidates(List<LearningItem> bucket, SenseKey? avoid)
    {
        if (avoid is null)
        {
            return bucket.Count;
        }

        int count = 0;

        foreach (LearningItem item in bucket)
        {
            if (!IsSameEntry(item, avoid))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsSameEntry(LearningItem item, SenseKey? avoid) =>
        avoid is not null &&
        string.Equals(item.Key.ListId, avoid.ListId, StringComparison.Ordinal) &&
        string.Equals(item.Key.EntryId, avoid.EntryId, StringComparison.Ordinal);

    /// <summary>
    /// 将一个义项移动到新的评级桶，使用 swap-remove 保持 O(1)。
    /// </summary>
    public void Update(SenseKey key, string ratingId)
    {
        Rating normalized = _scheme.Normalize(ratingId);

        if (!_positions.TryGetValue(key, out Position oldPosition))
        {
            throw new ArgumentException($"选择索引中不存在义项：{key}。", nameof(key));
        }

        if (string.Equals(oldPosition.RatingId, normalized.Id, StringComparison.Ordinal))
        {
            return;
        }

        List<LearningItem> oldBucket = _buckets[oldPosition.RatingId];
        LearningItem item = oldBucket[oldPosition.Index];
        int lastIndex = oldBucket.Count - 1;

        if (oldPosition.Index != lastIndex)
        {
            LearningItem moved = oldBucket[lastIndex];
            oldBucket[oldPosition.Index] = moved;
            _positions[moved.Key] = new Position(oldPosition.RatingId, oldPosition.Index);
        }

        oldBucket.RemoveAt(lastIndex);

        List<LearningItem> newBucket = _buckets[normalized.Id];
        _positions[key] = new Position(normalized.Id, newBucket.Count);
        newBucket.Add(item);
    }

    private readonly record struct Position(string RatingId, int Index);
}
