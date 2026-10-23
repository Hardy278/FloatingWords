using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public interface IVocabularyRepository
{
    IReadOnlyList<VocabularyListSummary> GetSummaries();

    VocabularyList? Load(string listId);

    bool Exists(string listId);

    void Save(VocabularyList list, VocabularySourceInfo source, bool overwrite);

    void Rename(string listId, string newName);

    void Delete(string listId);
}
