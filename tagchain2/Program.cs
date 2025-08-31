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
            HashSet<int> itemsInDb = new HashSet<int>();
            var connectionsInDb = new HashSet<(int FromId, int ToId, int TagId)>();


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

            // Getting items from main db
            try
            {
                userItems = await mySqlDb.GetUserListAsync(userName, startDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading items from MySQL: {ex.Message}");
                Environment.Exit(1);
            }

            /*
            I am loading all items and their connections from the main db and the tagchain db
            And then only adding to the tagchaindb those that are not there. This may use too much memory and it might be better
            to try to add every item from the maindb to the tagchaindb. this might be slower since i would need to make
            alot more calls to the database but i would required less memory.
            If I decide to change this i need find i way to still get the newitems to added them to the graph and the connections
            of the chains started prior to adding them to the db
            */

            // geting items from the tagchainDb
            try
            {
                itemsInDb = await sqliteDatabase.GetITems();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading items from sqlite: {ex.Message}");
                Environment.Exit(1);
            }

            // add new items to the tagchainDb
            try
            {
                Dictionary<int, Item> newItems = userItems
                .Where(kvp => !itemsInDb.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                connectionsInDb = await sqliteDatabase.GetConnections();
            }
            catch (Exception ex)
            {
                await sqliteDatabase.ClearItems();
                Console.WriteLine($"Error loading item connections from sqlite: {ex.Message}");
                Environment.Exit(3);
            }



            try
            {
                var newConnections = allConnections.Where(c => connectionsInDb.Contains((c.From.Id, c.To.Id, c.TagId))).ToList();
                await sqliteDatabase.AddItemsConnections(newConnections);
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




            // Graph graph = new Graph(userItems, allConnections);

            // graph.Start();
        }
    }
}