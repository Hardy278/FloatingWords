using FloatingWords.Application.Diagnostics;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Infrastructure.Sqlite;

namespace FloatingWords.Infrastructure.Vocabulary;

public static class StarterVocabularySeeder
{
    public static void SeedIfNeeded(SqliteDatabase database, IVocabularyRepository repository, IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(repository);

        logger ??= NullAppLogger.Instance;

        if (database.GetMeta("seeded") is not null)
        {
            return;
        }

        if (repository.GetSummaries().Count == 0)
        {
            repository.Save(
                StarterVocabulary.Create(),
                new VocabularySourceInfo("builtin", "内置示例"),
                overwrite: false);

            logger.Info("已写入内置示例词书。");
        }

        database.SetMeta("seeded", "1");
    }
}
