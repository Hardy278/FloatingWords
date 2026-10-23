using FloatingWords.Application.Vocabulary;
using FloatingWords.Domain.Vocabulary;
using Microsoft.Data.Sqlite;

namespace FloatingWords.Infrastructure.Sqlite;

public sealed class SqliteRatingStore(SqliteDatabase database) : IRatingStore
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public IReadOnlyDictionary<SenseKey, string> Load(string listId)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT entry_id, sense_id, rating_id
                FROM word_state
                WHERE list_id = $listId;
                """;
            command.Parameters.AddWithValue("$listId", listId);

            var states = new Dictionary<SenseKey, string>();

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                var key = new SenseKey(listId, reader.GetString(0), reader.GetString(1));
                states[key] = reader.GetString(2);
            }

            return states;
        }
        catch (Exception exception) when (exception is not RatingPersistenceException)
        {
            throw new RatingPersistenceException("读取评级状态失败。", exception);
        }
    }

    public void Save(SenseKey key, string ratingId, DateTimeOffset updatedAt)
    {
        try
        {
            using SqliteConnection connection = _database.OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO word_state (list_id, entry_id, sense_id, rating_id, updated_at)
                VALUES ($listId, $entryId, $senseId, $ratingId, $updatedAt)
                ON CONFLICT(list_id, entry_id, sense_id)
                DO UPDATE SET
                    rating_id = excluded.rating_id,
                    updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("$listId", key.ListId);
            command.Parameters.AddWithValue("$entryId", key.EntryId);
            command.Parameters.AddWithValue("$senseId", key.SenseId);
            command.Parameters.AddWithValue("$ratingId", ratingId);
            command.Parameters.AddWithValue("$updatedAt", updatedAt.ToUniversalTime().ToUnixTimeMilliseconds());
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (exception is not RatingPersistenceException)
        {
            throw new RatingPersistenceException("保存评级失败。", exception);
        }
    }
}
