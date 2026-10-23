using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public interface IRatingStore
{
    IReadOnlyDictionary<SenseKey, string> Load(string listId);

    void Save(SenseKey key, string ratingId, DateTimeOffset updatedAt);
}

public sealed class RatingPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
