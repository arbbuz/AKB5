using Microsoft.Data.Sqlite;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseSqliteConnectionFactory
    {
        public SqliteConnection OpenConnection(string databasePath)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false
            };

            var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys=ON;
                PRAGMA journal_mode=DELETE;
                """;
            command.ExecuteNonQuery();

            return connection;
        }
    }
}
