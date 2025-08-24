using System;
using System.Threading.Tasks;
using DatabaseHandling;
using Model;
using Microsoft.Extensions.Configuration;

namespace MyClassicApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string userName = "smemorato";
            DateOnly startDate = new DateOnly(2024, 06, 06);

            Dictionary<int, Item> userItems = new Dictionary<int, Item>();
            List<Edge> allConnections = new List<Edge>();


            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connStr = config.GetConnectionString("DefaultConnection");
            string mysqlConnectionString = config.GetConnectionString("Mysql") ?? throw new InvalidOperationException("Missing MySQL connection string.");

            string sqliteConnectionString = config.GetConnectionString("Sqlite") ?? throw new InvalidOperationException("Missing Sqlite connection string.");
            MySqlDatabase mySqlDb = new MySqlDatabase(mysqlConnectionString);
            SqliteDatabase sqliteDatabase = new SqliteDatabase(sqliteConnectionString);
            await sqliteDatabase.CreateDatabaseAsync();

            try
            {
                userItems = await mySqlDb.GetUserListAsync(userName, startDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading items from MySQL: {ex.Message}");
                Environment.Exit(1);
            }

            try
            {
                await sqliteDatabase.AddItems(userItems);
            }
            catch (Exception ex)
            {
                await sqliteDatabase.ClearItems();
                Console.WriteLine($"Error storing items in SQLite: {ex.Message}");
                Environment.Exit(2);
            }


            try
            {
                allConnections = await mySqlDb.GetVaConnection(userName, startDate, userItems);
            }
            catch (Exception ex)
            {
                await sqliteDatabase.ClearItems();
                Console.WriteLine($"Error loading item connections from MySQL: {ex.Message}");
                Environment.Exit(3);
            }

            try
            {
                await sqliteDatabase.AddItemsConnections(allConnections);
            }
            catch (Exception ex)
            {
                await sqliteDatabase.ClearItems();
                Console.WriteLine($"Error storing item connections in SQLite: {ex.Message}");
                Environment.Exit(4);
            }


            foreach (var item in userItems)
            {
                List<Edge> ItemConnections = allConnections.Where(ic => ic.From.Id == item.Value.Id).ToList();
                userItems[item.Key].AddEdgesOut(ItemConnections);
            }

            Console.WriteLine($" Successfully processed {userItems.Count} items and {allConnections.Count} connections.");




            //Graph graph = new Graph(userItems, allConnections);

            //graph.Start();
        }
    }
}