using System.Globalization;
using Microsoft.Data.Sqlite;

namespace FloatingWords.Infrastructure.Sqlite;

public sealed class SqliteDatabase(string databasePath)
{
    private static readonly string[] SchemaStatements =
    [
        """
        CREATE TABLE IF NOT EXISTS app_meta (
            meta_key   TEXT PRIMARY KEY,
            meta_value TEXT NOT NULL
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS vocabulary_list (
            list_id       TEXT PRIMARY KEY,
            name          TEXT NOT NULL,
            language      TEXT NOT NULL DEFAULT 'en',
            source_format TEXT NOT NULL DEFAULT 'json',
            source_name   TEXT,
            imported_at   INTEGER NOT NULL,
            updated_at    INTEGER NOT NULL
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS vocabulary_entry (
            list_id     TEXT NOT NULL,
            entry_id    TEXT NOT NULL,
            headword    TEXT NOT NULL,
            phonetic_uk TEXT,
            phonetic_us TEXT,
            position    INTEGER NOT NULL,
            PRIMARY KEY (list_id, entry_id),
            FOREIGN KEY (list_id) REFERENCES vocabulary_list(list_id) ON DELETE CASCADE
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS vocabulary_sense (
            list_id   TEXT NOT NULL,
            entry_id  TEXT NOT NULL,
            sense_id  TEXT NOT NULL,
            pos       TEXT,
            labels    TEXT NOT NULL DEFAULT '[]',
            position  INTEGER NOT NULL,
            PRIMARY KEY (list_id, entry_id, sense_id),
            FOREIGN KEY (list_id, entry_id)
                REFERENCES vocabulary_entry(list_id, entry_id) ON DELETE CASCADE
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS vocabulary_definition (
            list_id       TEXT NOT NULL,
            entry_id      TEXT NOT NULL,
            sense_id      TEXT NOT NULL,
            definition_id TEXT,
            text          TEXT,
            translation   TEXT,
            position      INTEGER NOT NULL,
            PRIMARY KEY (list_id, entry_id, sense_id, position),
            FOREIGN KEY (list_id, entry_id, sense_id)
                REFERENCES vocabulary_sense(list_id, entry_id, sense_id) ON DELETE CASCADE
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS vocabulary_example (
            list_id     TEXT NOT NULL,
            entry_id    TEXT NOT NULL,
            sense_id    TEXT NOT NULL,
            example_id  TEXT,
            text        TEXT,
            translation TEXT,
            position    INTEGER NOT NULL,
            PRIMARY KEY (list_id, entry_id, sense_id, position),
            FOREIGN KEY (list_id, entry_id, sense_id)
                REFERENCES vocabulary_sense(list_id, entry_id, sense_id) ON DELETE CASCADE
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS word_state (
            list_id    TEXT NOT NULL,
            entry_id   TEXT NOT NULL,
            sense_id   TEXT NOT NULL,
            rating_id  TEXT NOT NULL,
            updated_at INTEGER NOT NULL,
            PRIMARY KEY (list_id, entry_id, sense_id),
            FOREIGN KEY (list_id, entry_id, sense_id)
                REFERENCES vocabulary_sense(list_id, entry_id, sense_id) ON DELETE CASCADE
        ) WITHOUT ROWID;
        """,
        """
        CREATE INDEX IF NOT EXISTS ix_vocabulary_entry_list
            ON vocabulary_entry(list_id);
        """,
        """
        CREATE INDEX IF NOT EXISTS ix_vocabulary_sense_list
            ON vocabulary_sense(list_id, entry_id);
        """,
        """
        CREATE INDEX IF NOT EXISTS ix_word_state_list
            ON word_state(list_id);
        """
    ];

    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        DefaultTimeout = 5
    }.ToString();

    public string DatabasePath { get; } = databasePath ?? throw new ArgumentNullException(nameof(databasePath));

    public void Initialize()
    {
        string? directory = Path.GetDirectoryName(DatabasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using SqliteConnection connection = OpenConnection();

        using (SqliteCommand pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL;";
            pragma.ExecuteScalar();
        }

        using (SqliteCommand pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA synchronous = NORMAL;";
            pragma.ExecuteNonQuery();
        }

        foreach (string statement in SchemaStatements)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }

        using SqliteCommand versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        int version = Convert.ToInt32(versionCommand.ExecuteScalar(), CultureInfo.InvariantCulture);

        switch (version)
        {
            case > 1: throw new InvalidOperationException($"数据库 schema 版本 {version} 高于当前支持的版本 1。");
            case 0:
            {
                using SqliteCommand setVersion = connection.CreateCommand();
                setVersion.CommandText = "PRAGMA user_version = 1;";
                setVersion.ExecuteNonQuery();
                break;
            }
        }
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand foreignKeys = connection.CreateCommand();
        foreignKeys.CommandText = "PRAGMA foreign_keys = ON;";
        foreignKeys.ExecuteNonQuery();

        using SqliteCommand busyTimeout = connection.CreateCommand();
        busyTimeout.CommandText = "PRAGMA busy_timeout = 5000;";
        busyTimeout.ExecuteNonQuery();

        return connection;
    }

    public string? GetMeta(string key)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT meta_value FROM app_meta WHERE meta_key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", key);

        return command.ExecuteScalar() as string;
    }

    public void SetMeta(string key, string value)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO app_meta (meta_key, meta_value)
            VALUES ($key, $value)
            ON CONFLICT(meta_key) DO UPDATE SET meta_value = excluded.meta_value;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }
}
