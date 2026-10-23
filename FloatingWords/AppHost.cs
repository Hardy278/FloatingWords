using FloatingWords.Application.Diagnostics;
using FloatingWords.Application.Learning;
using FloatingWords.Application.Settings;
using FloatingWords.Application.Storage;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Infrastructure.Diagnostics;
using FloatingWords.Infrastructure.Settings;
using FloatingWords.Infrastructure.Sqlite;
using FloatingWords.Infrastructure.Storage;
using FloatingWords.Infrastructure.Vocabulary;

namespace FloatingWords;

/// <summary>
/// WPF 组合根。初始化数据目录、数据库、设置、词书库和会话工厂。
/// </summary>
public sealed class AppHost
{
    public AppHost()
    {
        DataDirectory = new PortableDataDirectory();
        DataDirectory.EnsureWritable();

        Logger = new FileAppLogger(DataDirectory.LogsPath);
        Logger.Info("FloatingWords 正在启动。");

        Database = new SqliteDatabase(DataDirectory.DatabasePath);
        Database.Initialize();

        Settings = new SettingsService(new JsonSettingsRepository(DataDirectory.SettingsPath), Logger);

        VocabularyRepository = new SqliteVocabularyRepository(Database);
        RatingStore = new SqliteRatingStore(Database);

        StarterVocabularySeeder.SeedIfNeeded(Database, VocabularyRepository, Logger);

        var readerRegistry = new VocabularyReaderRegistry([new JsonVocabularyReader()]);

        ImportService = new VocabularyImportService(readerRegistry, VocabularyRepository, Logger);
        VocabularyLists = new VocabularyListManager(VocabularyRepository, Settings, ImportService, Logger);
        Sessions = new LearningSessionFactory(RatingStore);
    }

    public IDataDirectory DataDirectory { get; }

    public IAppLogger Logger { get; }

    public SqliteDatabase Database { get; }

    public SettingsService Settings { get; }

    public SqliteVocabularyRepository VocabularyRepository { get; }

    public SqliteRatingStore RatingStore { get; }

    public VocabularyImportService ImportService { get; }

    public VocabularyListManager VocabularyLists { get; }

    public LearningSessionFactory Sessions { get; }
}
