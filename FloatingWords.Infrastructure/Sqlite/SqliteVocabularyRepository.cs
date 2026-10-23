using System.Text.Json;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Vocabulary;
using Microsoft.Data.Sqlite;

namespace FloatingWords.Infrastructure.Sqlite;

public sealed class SqliteVocabularyRepository(SqliteDatabase database, TimeProvider? clock = null) : IVocabularyRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public IReadOnlyList<VocabularyListSummary> GetSummaries()
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    l.list_id,
                    l.name,
                    l.language,
                    l.source_format,
                    l.source_name,
                    l.imported_at,
                    (SELECT COUNT(*) FROM vocabulary_entry e WHERE e.list_id = l.list_id),
                    (SELECT COUNT(*) FROM vocabulary_sense s WHERE s.list_id = l.list_id)
                FROM vocabulary_list l
                ORDER BY l.imported_at, l.list_id;
                """;

            var summaries = new List<VocabularyListSummary>();

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                summaries.Add(new VocabularyListSummary(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    (int)reader.GetInt64(6),
                    (int)reader.GetInt64(7),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5))));
            }

            return summaries;
        }
        catch (Exception exception) when (exception is not VocabularyStorageException)
        {
            throw new VocabularyStorageException("读取词书列表失败。", exception);
        }
    }

    public VocabularyList? Load(string listId)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();

            string listName;
            string language;

            using (SqliteCommand listCommand = connection.CreateCommand())
            {
                listCommand.CommandText =
                    """
                    SELECT name, language
                    FROM vocabulary_list
                    WHERE list_id = $listId
                    LIMIT 1;
                    """;
                listCommand.Parameters.AddWithValue("$listId", listId);

                using SqliteDataReader listReader = listCommand.ExecuteReader();

                if (!listReader.Read())
                {
                    return null;
                }

                listName = listReader.GetString(0);
                language = listReader.GetString(1);
            }

            var entryBuilders = new Dictionary<string, EntryBuilder>(StringComparer.Ordinal);
            var entryOrder = new List<string>();

            using (SqliteCommand entryCommand = connection.CreateCommand())
            {
                entryCommand.CommandText =
                    """
                    SELECT entry_id, headword, phonetic_uk, phonetic_us
                    FROM vocabulary_entry
                    WHERE list_id = $listId
                    ORDER BY position, entry_id;
                    """;
                entryCommand.Parameters.AddWithValue("$listId", listId);

                using SqliteDataReader entryReader = entryCommand.ExecuteReader();

                while (entryReader.Read())
                {
                    string entryId = entryReader.GetString(0);
                    var builder = new EntryBuilder(
                        entryId,
                        entryReader.GetString(1),
                        ReadNullableString(entryReader, 2),
                        ReadNullableString(entryReader, 3));

                    entryBuilders[entryId] = builder;
                    entryOrder.Add(entryId);
                }
            }

            var senseBuilders = new Dictionary<(string EntryId, string SenseId), SenseBuilder>();

            using (SqliteCommand senseCommand = connection.CreateCommand())
            {
                senseCommand.CommandText =
                    """
                    SELECT entry_id, sense_id, pos, labels
                    FROM vocabulary_sense
                    WHERE list_id = $listId
                    ORDER BY entry_id, position, sense_id;
                    """;
                senseCommand.Parameters.AddWithValue("$listId", listId);

                using SqliteDataReader senseReader = senseCommand.ExecuteReader();

                while (senseReader.Read())
                {
                    string entryId = senseReader.GetString(0);

                    if (!entryBuilders.TryGetValue(entryId, out EntryBuilder? entryBuilder))
                    {
                        continue;
                    }

                    var sense = new SenseBuilder(
                        senseReader.GetString(1),
                        ReadNullableString(senseReader, 2),
                        ParseLabels(ReadNullableString(senseReader, 3)));

                    entryBuilder.Senses.Add(sense);
                    senseBuilders[(entryId, sense.SenseId)] = sense;
                }
            }

            using (SqliteCommand definitionCommand = connection.CreateCommand())
            {
                definitionCommand.CommandText =
                    """
                    SELECT entry_id, sense_id, definition_id, text, translation
                    FROM vocabulary_definition
                    WHERE list_id = $listId
                    ORDER BY entry_id, sense_id, position;
                    """;
                definitionCommand.Parameters.AddWithValue("$listId", listId);

                using SqliteDataReader definitionReader = definitionCommand.ExecuteReader();

                while (definitionReader.Read())
                {
                    (string, string) key = (definitionReader.GetString(0), definitionReader.GetString(1));
                    if (!senseBuilders.TryGetValue(key, out SenseBuilder? sense))
                    {
                        continue;
                    }

                    sense.Definitions.Add(new Definition(
                        ReadNullableString(definitionReader, 2),
                        ReadNullableString(definitionReader, 3),
                        ReadNullableString(definitionReader, 4)));
                }
            }

            using (SqliteCommand exampleCommand = connection.CreateCommand())
            {
                exampleCommand.CommandText =
                    """
                    SELECT entry_id, sense_id, example_id, text, translation
                    FROM vocabulary_example
                    WHERE list_id = $listId
                    ORDER BY entry_id, sense_id, position;
                    """;
                exampleCommand.Parameters.AddWithValue("$listId", listId);

                using SqliteDataReader exampleReader = exampleCommand.ExecuteReader();

                while (exampleReader.Read())
                {
                    (string, string) key = (exampleReader.GetString(0), exampleReader.GetString(1));
                    if (!senseBuilders.TryGetValue(key, out SenseBuilder? sense))
                    {
                        continue;
                    }

                    sense.Examples.Add(new Example(
                        ReadNullableString(exampleReader, 2),
                        ReadNullableString(exampleReader, 3),
                        ReadNullableString(exampleReader, 4)));
                }
            }

            var entries = new List<VocabularyEntry>(entryOrder.Count);

            foreach (string entryId in entryOrder)
            {
                EntryBuilder entryBuilder = entryBuilders[entryId];

                VocabularySense[] senses = entryBuilder.Senses
                    .Select(sense => new VocabularySense(sense.SenseId, sense.PartOfSpeech, sense.Labels, sense.Definitions, sense.Examples))
                    .ToArray();

                Phonetic? phonetic = entryBuilder.PhoneticUk is null && entryBuilder.PhoneticUs is null
                    ? null : new Phonetic(entryBuilder.PhoneticUk, entryBuilder.PhoneticUs);

                entries.Add(new VocabularyEntry(
                    entryBuilder.EntryId,
                    entryBuilder.Headword,
                    phonetic,
                    senses));
            }

            return new VocabularyList(listId, listName, language, entries);
        }
        catch (Exception exception) when (exception is not VocabularyStorageException)
        {
            throw new VocabularyStorageException("读取词书失败。", exception);
        }
    }

    public bool Exists(string listId)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            return Exists(connection, null, listId);
        }
        catch (Exception exception) when (exception is not VocabularyStorageException)
        {
            throw new VocabularyStorageException("检查词书是否存在失败。", exception);
        }
    }

    public void Save(VocabularyList list, VocabularySourceInfo source, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(source);

        IReadOnlyList<string> ruleIssues = VocabularyListRules.Validate(list);

        if (ruleIssues.Count > 0)
        {
            throw new ArgumentException("词书内容不合法：" + string.Join(" ", ruleIssues), nameof(list));
        }

        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            bool exists = Exists(connection, transaction, list.ListId);

            if (exists && !overwrite)
            {
                throw new VocabularyConflictException(list.ListId);
            }

            long now = _clock.GetUtcNow().ToUnixTimeMilliseconds();
            IReadOnlyList<(string EntryId, string SenseId)> oldSenseKeys = exists
                ? ReadSenseKeys(connection, transaction, list.ListId)
                : Array.Empty<(string EntryId, string SenseId)>();
            IReadOnlyList<string> oldEntryIds = exists
                ? ReadEntryIds(connection, transaction, list.ListId)
                : Array.Empty<string>();

            if (exists)
            {
                using SqliteCommand updateList = connection.CreateCommand();
                updateList.Transaction = transaction;
                updateList.CommandText =
                    """
                    UPDATE vocabulary_list
                    SET name = $name,
                        language = $language,
                        source_format = $sourceFormat,
                        source_name = $sourceName,
                        updated_at = $updatedAt
                    WHERE list_id = $listId;
                    """;
                updateList.Parameters.AddWithValue("$name", list.ListName);
                updateList.Parameters.AddWithValue("$language", list.Language);
                updateList.Parameters.AddWithValue("$sourceFormat", source.Format);
                updateList.Parameters.AddWithValue(
                    "$sourceName",
                    (object?)source.Name ?? DBNull.Value);
                updateList.Parameters.AddWithValue("$updatedAt", now);
                updateList.Parameters.AddWithValue("$listId", list.ListId);
                updateList.ExecuteNonQuery();
            }
            else
            {
                using SqliteCommand insertList = connection.CreateCommand();
                insertList.Transaction = transaction;
                insertList.CommandText =
                    """
                    INSERT INTO vocabulary_list
                        (list_id, name, language, source_format, source_name, imported_at, updated_at)
                    VALUES
                        ($listId, $name, $language, $sourceFormat, $sourceName, $importedAt, $updatedAt);
                    """;
                insertList.Parameters.AddWithValue("$listId", list.ListId);
                insertList.Parameters.AddWithValue("$name", list.ListName);
                insertList.Parameters.AddWithValue("$language", list.Language);
                insertList.Parameters.AddWithValue("$sourceFormat", source.Format);
                insertList.Parameters.AddWithValue(
                    "$sourceName",
                    (object?)source.Name ?? DBNull.Value);
                insertList.Parameters.AddWithValue("$importedAt", now);
                insertList.Parameters.AddWithValue("$updatedAt", now);
                insertList.ExecuteNonQuery();
            }

            var incomingSenseKeys = new HashSet<(string EntryId, string SenseId)>();
            var incomingEntryIds = new HashSet<string>(StringComparer.Ordinal);

            for (int entryIndex = 0; entryIndex < list.Entries.Count; entryIndex++)
            {
                VocabularyEntry entry = list.Entries[entryIndex];
                incomingEntryIds.Add(entry.EntryId);

                UpsertEntry(connection, transaction, list.ListId, entry, entryIndex);

                for (int senseIndex = 0; senseIndex < entry.Senses.Count; senseIndex++)
                {
                    VocabularySense sense = entry.Senses[senseIndex];
                    incomingSenseKeys.Add((entry.EntryId, sense.SenseId));

                    UpsertSense(connection, transaction, list.ListId, entry.EntryId, sense, senseIndex);

                    ReplaceDefinitions(connection, transaction, list.ListId, entry.EntryId, sense);

                    ReplaceExamples(connection, transaction, list.ListId, entry.EntryId, sense);
                }
            }

            foreach ((string EntryId, string SenseId) oldSense in oldSenseKeys)
            {
                if (incomingSenseKeys.Contains(oldSense))
                {
                    continue;
                }

                using SqliteCommand deleteSense = connection.CreateCommand();
                deleteSense.Transaction = transaction;
                deleteSense.CommandText =
                    """
                    DELETE FROM vocabulary_sense
                    WHERE list_id = $listId AND entry_id = $entryId AND sense_id = $senseId;
                    """;
                deleteSense.Parameters.AddWithValue("$listId", list.ListId);
                deleteSense.Parameters.AddWithValue("$entryId", oldSense.EntryId);
                deleteSense.Parameters.AddWithValue("$senseId", oldSense.SenseId);
                deleteSense.ExecuteNonQuery();
            }

            foreach (string oldEntryId in oldEntryIds)
            {
                if (incomingEntryIds.Contains(oldEntryId))
                {
                    continue;
                }

                using SqliteCommand deleteEntry = connection.CreateCommand();
                deleteEntry.Transaction = transaction;
                deleteEntry.CommandText =
                    """
                    DELETE FROM vocabulary_entry
                    WHERE list_id = $listId AND entry_id = $entryId;
                    """;
                deleteEntry.Parameters.AddWithValue("$listId", list.ListId);
                deleteEntry.Parameters.AddWithValue("$entryId", oldEntryId);
                deleteEntry.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception exception) when (exception is not (VocabularyConflictException or ArgumentException or VocabularyStorageException))
        {
            throw new VocabularyStorageException("保存词书失败。", exception);
        }
    }

    public void Rename(string listId, string newName)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE vocabulary_list
                SET name = $name, updated_at = $updatedAt
                WHERE list_id = $listId;
                """;
            command.Parameters.AddWithValue("$name", newName);
            command.Parameters.AddWithValue("$updatedAt", _clock.GetUtcNow().ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$listId", listId);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new VocabularyNotFoundException(listId);
            }
        }
        catch (Exception exception) when (
            exception is not (VocabularyNotFoundException or VocabularyStorageException))
        {
            throw new VocabularyStorageException("重命名词书失败。", exception);
        }
    }

    public void Delete(string listId)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM vocabulary_list WHERE list_id = $listId;";
            command.Parameters.AddWithValue("$listId", listId);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new VocabularyNotFoundException(listId);
            }
        }
        catch (Exception exception) when (exception is not (VocabularyNotFoundException or VocabularyStorageException))
        {
            throw new VocabularyStorageException("删除词书失败。", exception);
        }
    }

    private static bool Exists(SqliteConnection connection, SqliteTransaction? transaction, string listId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM vocabulary_list WHERE list_id = $listId LIMIT 1;";
        command.Parameters.AddWithValue("$listId", listId);
        return command.ExecuteScalar() is not null;
    }

    private static List<(string EntryId, string SenseId)> ReadSenseKeys(
        SqliteConnection connection, SqliteTransaction transaction, string listId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT entry_id, sense_id FROM vocabulary_sense WHERE list_id = $listId;";
        command.Parameters.AddWithValue("$listId", listId);

        var keys = new List<(string EntryId, string SenseId)>();
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            keys.Add((reader.GetString(0), reader.GetString(1)));
        }

        return keys;
    }

    private static List<string> ReadEntryIds(SqliteConnection connection, SqliteTransaction transaction, string listId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT entry_id FROM vocabulary_entry WHERE list_id = $listId;";
        command.Parameters.AddWithValue("$listId", listId);

        var ids = new List<string>();
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static void UpsertEntry(SqliteConnection connection, SqliteTransaction transaction,
        string listId, VocabularyEntry entry, int position)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO vocabulary_entry
                (list_id, entry_id, headword, phonetic_uk, phonetic_us, position)
            VALUES
                ($listId, $entryId, $headword, $phoneticUk, $phoneticUs, $position)
            ON CONFLICT(list_id, entry_id) DO UPDATE SET
                headword = excluded.headword,
                phonetic_uk = excluded.phonetic_uk,
                phonetic_us = excluded.phonetic_us,
                position = excluded.position;
            """;
        command.Parameters.AddWithValue("$listId", listId);
        command.Parameters.AddWithValue("$entryId", entry.EntryId);
        command.Parameters.AddWithValue("$headword", entry.Headword);
        command.Parameters.AddWithValue("$phoneticUk", (object?)entry.Phonetic?.Uk ?? DBNull.Value);
        command.Parameters.AddWithValue("$phoneticUs", (object?)entry.Phonetic?.Us ?? DBNull.Value);
        command.Parameters.AddWithValue("$position", position);
        command.ExecuteNonQuery();
    }

    private static void UpsertSense(SqliteConnection connection, SqliteTransaction transaction,
        string listId, string entryId, VocabularySense sense, int position)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO vocabulary_sense
                (list_id, entry_id, sense_id, pos, labels, position)
            VALUES
                ($listId, $entryId, $senseId, $pos, $labels, $position)
            ON CONFLICT(list_id, entry_id, sense_id) DO UPDATE SET
                pos = excluded.pos,
                labels = excluded.labels,
                position = excluded.position;
            """;
        command.Parameters.AddWithValue("$listId", listId);
        command.Parameters.AddWithValue("$entryId", entryId);
        command.Parameters.AddWithValue("$senseId", sense.SenseId);
        command.Parameters.AddWithValue("$pos", (object?)sense.PartOfSpeech ?? DBNull.Value);
        command.Parameters.AddWithValue("$labels", JsonSerializer.Serialize(sense.Labels));
        command.Parameters.AddWithValue("$position", position);
        command.ExecuteNonQuery();
    }

    private static void ReplaceDefinitions(SqliteConnection connection, SqliteTransaction transaction,
        string listId, string entryId, VocabularySense sense)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText =
                """
                DELETE FROM vocabulary_definition
                WHERE list_id = $listId AND entry_id = $entryId AND sense_id = $senseId;
                """;
            delete.Parameters.AddWithValue("$listId", listId);
            delete.Parameters.AddWithValue("$entryId", entryId);
            delete.Parameters.AddWithValue("$senseId", sense.SenseId);
            delete.ExecuteNonQuery();
        }

        for (int index = 0; index < sense.Definitions.Count; index++)
        {
            Definition definition = sense.Definitions[index];

            using SqliteCommand insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO vocabulary_definition
                    (list_id, entry_id, sense_id, definition_id, text, translation, position)
                VALUES
                    ($listId, $entryId, $senseId, $definitionId, $text, $translation, $position);
                """;
            insert.Parameters.AddWithValue("$listId", listId);
            insert.Parameters.AddWithValue("$entryId", entryId);
            insert.Parameters.AddWithValue("$senseId", sense.SenseId);
            insert.Parameters.AddWithValue("$definitionId", (object?)definition.DefinitionId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$text", (object?)definition.Text ?? DBNull.Value);
            insert.Parameters.AddWithValue("$translation", (object?)definition.Translation ?? DBNull.Value);
            insert.Parameters.AddWithValue("$position", index);
            insert.ExecuteNonQuery();
        }
    }

    private static void ReplaceExamples(SqliteConnection connection, SqliteTransaction transaction,
        string listId, string entryId, VocabularySense sense)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText =
                """
                DELETE FROM vocabulary_example
                WHERE list_id = $listId AND entry_id = $entryId AND sense_id = $senseId;
                """;
            delete.Parameters.AddWithValue("$listId", listId);
            delete.Parameters.AddWithValue("$entryId", entryId);
            delete.Parameters.AddWithValue("$senseId", sense.SenseId);
            delete.ExecuteNonQuery();
        }

        for (int index = 0; index < sense.Examples.Count; index++)
        {
            Example example = sense.Examples[index];

            using SqliteCommand insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO vocabulary_example
                    (list_id, entry_id, sense_id, example_id, text, translation, position)
                VALUES
                    ($listId, $entryId, $senseId, $exampleId, $text, $translation, $position);
                """;
            insert.Parameters.AddWithValue("$listId", listId);
            insert.Parameters.AddWithValue("$entryId", entryId);
            insert.Parameters.AddWithValue("$senseId", sense.SenseId);
            insert.Parameters.AddWithValue("$exampleId", (object?)example.ExampleId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$text", (object?)example.Text ?? DBNull.Value);
            insert.Parameters.AddWithValue("$translation", (object?)example.Translation ?? DBNull.Value);
            insert.Parameters.AddWithValue("$position", index);
            insert.ExecuteNonQuery();
        }
    }

    private static string[] ParseLabels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private sealed class EntryBuilder(string entryId, string headword, string? phoneticUk, string? phoneticUs)
    {
        public string EntryId { get; } = entryId;

        public string Headword { get; } = headword;

        public string? PhoneticUk { get; } = phoneticUk;

        public string? PhoneticUs { get; } = phoneticUs;

        public List<SenseBuilder> Senses { get; } = [];
    }

    private sealed class SenseBuilder(string senseId, string? partOfSpeech, IReadOnlyList<string> labels)
    {
        public string SenseId { get; } = senseId;

        public string? PartOfSpeech { get; } = partOfSpeech;

        public IReadOnlyList<string> Labels { get; } = labels;

        public List<Definition> Definitions { get; } = [];

        public List<Example> Examples { get; } = [];
    }
}
