using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Ratings;
using FloatingWords.Domain.Scheduling;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Learning;

public sealed class LearningSessionFactory(IRatingStore ratingStore, RatingScheme? ratingScheme = null, IRandomSource? random = null, TimeProvider? clock = null)
{
    private readonly IRatingStore _ratingStore = ratingStore ?? throw new ArgumentNullException(nameof(ratingStore));
    private readonly RatingScheme _ratingScheme = ratingScheme ?? RatingScheme.Default;
    private readonly IRandomSource _random = random ?? SystemRandomSource.Shared;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public RotationService Create(VocabularyList list, SenseKey? avoid = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        IReadOnlyDictionary<SenseKey, string> states = _ratingStore.Load(list.ListId);
        IReadOnlyList<LearningItem> items = LearningItemFactory.Flatten(list);
        var index = new SelectionIndex(items, states, _ratingScheme, _random);

        return new RotationService(index, _ratingStore, _ratingScheme, _clock, avoid);
    }
}
