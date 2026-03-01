using Microsoft.Data.Sqlite;
using InventoryApp.Models;
using System.Collections.Generic;

namespace InventoryApp.Data
{
    public static class DatabaseHelper
    {
        private const string ConnectionString = "Data Source=inventory.db";

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Inventory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Weight DECIMAL(18,2) NOT NULL,
                        Purity TEXT NOT NULL,
                        Price DECIMAL(18,2) NOT NULL,
                        CreatedDate DATETIME NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        public static List<InventoryItem> GetAllItems()
        {
            var items = new List<InventoryItem>();
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Inventory ORDER BY CreatedDate DESC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new InventoryItem
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Weight = reader.GetDecimal(2),
                            Purity = reader.GetString(3),
                            Price = reader.GetDecimal(4),
                            CreatedDate = reader.GetDateTime(5)
                        });
                    }
                }
            }
            return items;
        }

        public static void AddItem(InventoryItem item)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Inventory (Name, Weight, Purity, Price, CreatedDate) 
                    VALUES ($name, $weight, $purity, $price, $date)";
                command.Parameters.AddWithValue("$name", item.Name);
                command.Parameters.AddWithValue("$weight", item.Weight);
                command.Parameters.AddWithValue("$purity", item.Purity);
                command.Parameters.AddWithValue("$price", item.Price);
                command.Parameters.AddWithValue("$date", item.CreatedDate);
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateItem(InventoryItem item)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Inventory 
                    SET Name = $name, Weight = $weight, Purity = $purity, Price = $price 
                    WHERE Id = $id";
                command.Parameters.AddWithValue("$name", item.Name);
                command.Parameters.AddWithValue("$weight", item.Weight);
                command.Parameters.AddWithValue("$purity", item.Purity);
                command.Parameters.AddWithValue("$price", item.Price);
                command.Parameters.AddWithValue("$id", item.Id);
                command.ExecuteNonQuery();
            }
        }

        public static void DeleteItem(int id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Inventory WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public static (int TotalItems, decimal TotalValue) GetSummary()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*), SUM(Price) FROM Inventory";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int count = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        decimal total = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                        return (count, total);
                    }
                }
            }
            return (0, 0);
        }
    }
}
