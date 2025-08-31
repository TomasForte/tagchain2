using System.Data.SQLite;
using System.Transactions;
using Model;

namespace DatabaseHandling
{
    public class SqliteDatabase
    {
        private readonly string _connectionString;

        public SqliteDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<SQLiteConnection> GetOpenConnectionAsync()
        {
            var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            // I need to enable foreign keys constraint in every connection in sqlite
            await using (var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            return connection;
        }

        public async Task CreateDatabaseAsync()
        {
            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new SQLiteCommand(dbConnection))
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS items (
                            id INTEGER PRIMARY KEY,
                            title TEXT,
                            finishDate TEXT
                        )";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS itemsConnections (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            itemId1 INTEGER,
                            itemId2 INTEGER,
                            tagId INTEGER,
                            FOREIGN KEY(itemId1) REFERENCES items(id) ON DELETE CASCADE,
                            FOREIGN KEY(itemId2) REFERENCES items(id) ON DELETE CASCADE
                        )";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS chains (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            parentId INTEGER,
                            itemId INTEGER,
                            tagid INTEGER,
                            usedItemIds TEXT,
                            usedTagIds TEXT,
                            FOREIGN KEY(itemId) REFERENCES items(id)
                        )";
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task AddItemsConnections(List<Edge> edges)
        {
            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                using (var transaction = dbConnection.BeginTransaction())
                {
                    try
                    {
                        foreach (var edge in edges)
                        {
                            await using (var command = new SQLiteCommand(@"
                                    INSERT INTO itemsConnections (itemId1, itemId2, tagId)
                                    VALUES (@itemId1, @itemId2, @tagId);
                                ", dbConnection, transaction))
                            {

                                command.Parameters.AddWithValue("@itemId1", edge.From.Id);
                                command.Parameters.AddWithValue("@itemId2", edge.To.Id);
                                command.Parameters.AddWithValue("@tagId", edge.TagId);

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Insert itemsConnections Failed: {ex.Message}");
                        transaction.Rollback();
                        throw;
                    }
                }


            }
        }

        
        public async Task<HashSet<(int FromId, int ToId, int TagId)>> GetConnections()
        {
            var connections = new HashSet<(int FromId, int ToId, int TagId)>();

            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new SQLiteCommand(dbConnection))
                {
                    command.CommandText = @"
                        SELECT itemId1, itemId2, tagd_id
                        FROM itemConnections;
                    ";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fromId = reader.GetInt32(0);
                            int toId = reader.GetInt32(1);
                            int tagId = reader.GetInt32(2);
                            connections.Add((fromId, toId, tagId));

                        }
                    }
                }
            }
            return connections;
        }


        public async Task AddItems(Dictionary<int, Item> items)
        {
            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                using (var transaction = dbConnection.BeginTransaction())
                {
                    try
                    {

                        foreach (var item in items)
                        {
                            await using (var command = new SQLiteCommand(@"
                                    INSERT INTO items (id, title, finishDate)
                                    VALUES (@id, @title, @finishDate);
                                ", dbConnection, transaction))
                            {

                                command.Parameters.AddWithValue("@id", item.Value.Id);
                                command.Parameters.AddWithValue("@title", item.Value.Title);
                                command.Parameters.AddWithValue("@finishDate", item.Value.Date);

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Insert items Failed: {ex.Message}");
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<HashSet<int>> GetITems()
        {
            HashSet<int> items = new HashSet<int>();

            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new SQLiteCommand(dbConnection))
                {
                    command.CommandText = @"
                        SELECT id
                        FROM items;
                    ";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(reader.GetInt32(0));

                        }
                    }
                }
            }
            return items;
        }


        public async Task ClearItems()
        {
            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new SQLiteCommand(dbConnection))
                {
                    command.CommandText = @"
                        DELETE FROM items;
                        DELETE FROM SQLITE_SEQUENCE WHERE name='items';
                    ";

                    await command.ExecuteNonQueryAsync();
                }

            }
        }
    }
}