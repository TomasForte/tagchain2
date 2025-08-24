using System;
using MySqlConnector;
using Model;
using System.Data.Common;
using System.Security.Cryptography;

namespace DatabaseHandling
{
    public class MySqlDatabase
    {
        private readonly string _connectionString;

        public MySqlDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }


        public async Task<Dictionary<int, Item>> GetUserListAsync(string userName, DateOnly startDate)
        {
            var Items = new Dictionary<int, Item>();

            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new MySqlCommand("GetUserList", dbConnection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@userName", userName);
                    command.Parameters.AddWithValue("@startDate", startDate);

                    await using (var resultReader = await command.ExecuteReaderAsync())
                    {
                        while (await resultReader.ReadAsync())
                        {
                            int id = resultReader.GetInt32("id");
                            string title = resultReader.GetString("title");
                            DateOnly finishDate = resultReader.GetDateOnly("finishDate");
                            if (!Items.ContainsKey(id))
                            {
                                Items[id] = new Item(
                                            id,
                                            title,
                                            finishDate
                                            );
                            }
                            else
                            {
                                 throw new InvalidOperationException($"Duplicate item detected for mal_id: {id}");
                            }

                        }
                    }
                }
            }
            return Items;
        }
        


        public async Task<List<Edge>> GetVaConnection(string userName, DateOnly startDate, Dictionary<int, Item> userItems)
        {
            var Edges = new List<Edge>();

            await using (var dbConnection = await GetOpenConnectionAsync())
            {
                await using (var command = new MySqlCommand("GetVoiceActorChains", dbConnection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@userName", userName);
                    command.Parameters.AddWithValue("@startDate", startDate);

                    await using (var resultReader = await command.ExecuteReaderAsync())
                    {
                        while (await resultReader.ReadAsync())
                        {
                            int id1 = resultReader.GetInt32("id1");
                            int id2 = resultReader.GetInt32("id2");
                            int tagId = resultReader.GetInt32("person_id");
                            Edges.Add(new Edge(
                                        userItems[id1],
                                        userItems[id2],
                                        tagId
                                        )
                            );  
                        }
                    }
                }
            }
            return Edges;
        }



    }   
    
}