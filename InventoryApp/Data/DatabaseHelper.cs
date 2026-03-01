using Microsoft.Data.Sqlite;
using InventoryApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InventoryApp.Data
{
    public static class DatabaseHelper
    {
        private static string DbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "jms.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static void InitializeDatabase()
        {
            var dir = Path.GetDirectoryName(DbPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var conn = Open();
            Exec(conn, @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key   TEXT PRIMARY KEY,
                    Value TEXT NOT NULL DEFAULT ''
                );
                CREATE TABLE IF NOT EXISTS Suppliers (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Categories (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name      TEXT NOT NULL,
                    MetalType TEXT NOT NULL DEFAULT 'Gold'
                );
                CREATE TABLE IF NOT EXISTS Inventory (
                    SKU         TEXT PRIMARY KEY,
                    Name        TEXT NOT NULL,
                    CategoryId  INTEGER NOT NULL,
                    Purity      TEXT NOT NULL,
                    GrossWt     REAL NOT NULL DEFAULT 0,
                    StoneWt     REAL NOT NULL DEFAULT 0,
                    NetWt       REAL NOT NULL DEFAULT 0,
                    IsSold      INTEGER NOT NULL DEFAULT 0,
                    CostPrice   REAL NOT NULL DEFAULT 0,
                    CreatedDate TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Customers (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name   TEXT NOT NULL,
                    Mobile TEXT NOT NULL DEFAULT ''
                );
                CREATE TABLE IF NOT EXISTS Transactions (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date          TEXT NOT NULL,
                    CustomerId    INTEGER NOT NULL DEFAULT 0,
                    CustomerName  TEXT NOT NULL DEFAULT '',
                    TotalAmount   REAL NOT NULL DEFAULT 0,
                    LineItemsJson TEXT NOT NULL DEFAULT '[]'
                );
            ");

            // Seed default metal rates if not present
            foreach (var key in new[] { "Gold24K", "Gold22K", "Gold18K", "Silver" })
                Exec(conn, $"INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('{key}','0')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('ShopName','Reddy Jewellery')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('ShopAddress','')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('GSTNumber','')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('LabelPrinter','')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('ReceiptPrinter','')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('AutoBackup','0')");
            Exec(conn, "INSERT OR IGNORE INTO Settings (Key,Value) VALUES ('BackupPath','')");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        public static SqliteConnection Open()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        private static void Exec(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        // ── Settings ─────────────────────────────────────────────────────────────

        public static string GetSetting(string key, string defaultValue = "")
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key=$k";
            cmd.Parameters.AddWithValue("$k", key);
            var result = cmd.ExecuteScalar();
            return result is string s ? s : defaultValue;
        }

        public static void SetSetting(string key, string value)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key,Value) VALUES ($k,$v)";
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }

        public static MetalRates GetMetalRates()
        {
            return new MetalRates
            {
                Gold24K = decimal.Parse(GetSetting("Gold24K", "0")),
                Gold22K = decimal.Parse(GetSetting("Gold22K", "0")),
                Gold18K = decimal.Parse(GetSetting("Gold18K", "0")),
                Silver  = decimal.Parse(GetSetting("Silver",  "0")),
            };
        }

        public static void SaveMetalRates(MetalRates rates)
        {
            SetSetting("Gold24K", rates.Gold24K.ToString());
            SetSetting("Gold22K", rates.Gold22K.ToString());
            SetSetting("Gold18K", rates.Gold18K.ToString());
            SetSetting("Silver",  rates.Silver.ToString());
        }

        // ── Categories ───────────────────────────────────────────────────────────

        public static List<Category> GetCategories()
        {
            var list = new List<Category>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, MetalType FROM Categories ORDER BY Name";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Category { Id = r.GetInt32(0), Name = r.GetString(1), MetalType = Enum.Parse<MetalType>(r.GetString(2)) });
            return list;
        }

        public static void AddCategory(Category c)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories (Name,MetalType) VALUES ($n,$m)";
            cmd.Parameters.AddWithValue("$n", c.Name);
            cmd.Parameters.AddWithValue("$m", c.MetalType.ToString());
            cmd.ExecuteNonQuery();
        }

        public static void UpdateCategory(Category c)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Categories SET Name=$n, MetalType=$m WHERE Id=$id";
            cmd.Parameters.AddWithValue("$n", c.Name);
            cmd.Parameters.AddWithValue("$m", c.MetalType.ToString());
            cmd.Parameters.AddWithValue("$id", c.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteCategory(int id)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Categories WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── SKU Generation ───────────────────────────────────────────────────────

        public static string GenerateSKU(string categoryPrefix)
        {
            var year = DateTime.Now.Year;
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*)+1 FROM Inventory WHERE SKU LIKE $prefix";
            cmd.Parameters.AddWithValue("$prefix", $"{categoryPrefix}-{year}-%");
            var seq = (long)(cmd.ExecuteScalar() ?? 1L);
            return $"{categoryPrefix}-{year}-{seq:D3}";
        }

        // ── Inventory ────────────────────────────────────────────────────────────

        public static List<InventoryItem> GetInventoryItems(string? search = null, int categoryId = 0, bool? isSold = null)
        {
            var list = new List<InventoryItem>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add("(i.SKU LIKE $s OR i.Name LIKE $s)");
                cmd.Parameters.AddWithValue("$s", $"%{search}%");
            }
            if (categoryId > 0) { where.Add("i.CategoryId=$cid"); cmd.Parameters.AddWithValue("$cid", categoryId); }
            if (isSold.HasValue)  { where.Add("i.IsSold=$sold");    cmd.Parameters.AddWithValue("$sold", isSold.Value ? 1 : 0); }
            var wClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            cmd.CommandText = $@"SELECT i.SKU,i.Name,i.CategoryId,c.Name,i.Purity,i.GrossWt,i.StoneWt,i.NetWt,i.IsSold,i.CostPrice,i.CreatedDate
                                 FROM Inventory i LEFT JOIN Categories c ON c.Id=i.CategoryId {wClause} ORDER BY i.CreatedDate DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InventoryItem
                {
                    SKU = r.GetString(0), Name = r.GetString(1), CategoryId = r.GetInt32(2),
                    CategoryName = r.IsDBNull(3) ? "" : r.GetString(3),
                    Purity = Enum.Parse<PurityType>(r.GetString(4)),
                    GrossWt = r.GetDecimal(5), StoneWt = r.GetDecimal(6), NetWt = r.GetDecimal(7),
                    IsSold = r.GetInt32(8) == 1, CostPrice = r.GetDecimal(9),
                    CreatedDate = DateTime.Parse(r.GetString(10))
                });
            return list;
        }

        public static InventoryItem? GetItemBySKU(string sku)
        {
            return GetInventoryItems(search: sku).FirstOrDefault(i => i.SKU == sku);
        }

        public static void AddInventoryItem(InventoryItem item)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Inventory (SKU,Name,CategoryId,Purity,GrossWt,StoneWt,NetWt,IsSold,CostPrice,CreatedDate)
                                VALUES ($sku,$n,$cid,$p,$gw,$sw,$nw,$sold,$cp,$dt)";
            cmd.Parameters.AddWithValue("$sku",  item.SKU);
            cmd.Parameters.AddWithValue("$n",    item.Name);
            cmd.Parameters.AddWithValue("$cid",  item.CategoryId);
            cmd.Parameters.AddWithValue("$p",    item.Purity.ToString());
            cmd.Parameters.AddWithValue("$gw",   item.GrossWt);
            cmd.Parameters.AddWithValue("$sw",   item.StoneWt);
            cmd.Parameters.AddWithValue("$nw",   item.NetWt);
            cmd.Parameters.AddWithValue("$sold", item.IsSold ? 1 : 0);
            cmd.Parameters.AddWithValue("$cp",   item.CostPrice);
            cmd.Parameters.AddWithValue("$dt",   item.CreatedDate.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public static void UpdateInventoryItem(InventoryItem item)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Inventory SET Name=$n,CategoryId=$cid,Purity=$p,GrossWt=$gw,StoneWt=$sw,NetWt=$nw,CostPrice=$cp WHERE SKU=$sku";
            cmd.Parameters.AddWithValue("$n",   item.Name);
            cmd.Parameters.AddWithValue("$cid", item.CategoryId);
            cmd.Parameters.AddWithValue("$p",   item.Purity.ToString());
            cmd.Parameters.AddWithValue("$gw",  item.GrossWt);
            cmd.Parameters.AddWithValue("$sw",  item.StoneWt);
            cmd.Parameters.AddWithValue("$nw",  item.NetWt);
            cmd.Parameters.AddWithValue("$cp",  item.CostPrice);
            cmd.Parameters.AddWithValue("$sku", item.SKU);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteInventoryItem(string sku)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Inventory WHERE SKU=$sku";
            cmd.Parameters.AddWithValue("$sku", sku);
            cmd.ExecuteNonQuery();
        }

        public static void MarkAsSold(string sku)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Inventory SET IsSold=1 WHERE SKU=$sku";
            cmd.Parameters.AddWithValue("$sku", sku);
            cmd.ExecuteNonQuery();
        }

        // ── Dashboard Summary ─────────────────────────────────────────────────────

        public static (int TotalCount, decimal GoldWeight, decimal SilverWeight, decimal TotalValue) GetDashboardSummary(MetalRates rates)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT i.Purity, SUM(i.NetWt), c.MetalType FROM Inventory i 
                                LEFT JOIN Categories c ON c.Id=i.CategoryId
                                WHERE i.IsSold=0 GROUP BY i.Purity";
            decimal goldWt = 0, silverWt = 0, totalVal = 0;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var purity    = Enum.Parse<PurityType>(r.GetString(0));
                var wt        = r.GetDecimal(1);
                var metalType = r.IsDBNull(2) ? "Gold" : r.GetString(2);
                if (metalType == "Silver") silverWt += wt; else goldWt += wt;
                totalVal += wt * rates.GetRate(purity);
            }

            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = "SELECT COUNT(*) FROM Inventory WHERE IsSold=0";
            int count = Convert.ToInt32(cmd2.ExecuteScalar() ?? 0);
            return (count, goldWt, silverWt, totalVal);
        }

        // ── Customers ────────────────────────────────────────────────────────────

        public static List<Customer> SearchCustomers(string query)
        {
            var list = new List<Customer>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Mobile FROM Customers WHERE Name LIKE $q OR Mobile LIKE $q LIMIT 10";
            cmd.Parameters.AddWithValue("$q", $"%{query}%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Customer { Id = r.GetInt32(0), Name = r.GetString(1), Mobile = r.GetString(2) });
            return list;
        }

        public static int AddOrGetCustomer(Customer c)
        {
            using var conn = Open();
            if (!string.IsNullOrWhiteSpace(c.Mobile))
            {
                using var find = conn.CreateCommand();
                find.CommandText = "SELECT Id FROM Customers WHERE Mobile=$m";
                find.Parameters.AddWithValue("$m", c.Mobile);
                var existing = find.ExecuteScalar();
                if (existing != null) return Convert.ToInt32(existing);
            }
            using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT INTO Customers (Name,Mobile) VALUES ($n,$m) RETURNING Id";
            ins.Parameters.AddWithValue("$n", c.Name);
            ins.Parameters.AddWithValue("$m", c.Mobile);
            return Convert.ToInt32(ins.ExecuteScalar() ?? 0);
        }

        // ── Transactions ──────────────────────────────────────────────────────────

        public static void AddTransaction(TransactionRecord tx)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Transactions (Date,CustomerId,CustomerName,TotalAmount,LineItemsJson)
                                VALUES ($dt,$cid,$cn,$total,$json)";
            cmd.Parameters.AddWithValue("$dt",    tx.Date.ToString("o"));
            cmd.Parameters.AddWithValue("$cid",   tx.CustomerId);
            cmd.Parameters.AddWithValue("$cn",    tx.CustomerName);
            cmd.Parameters.AddWithValue("$total", tx.TotalAmount);
            cmd.Parameters.AddWithValue("$json",  tx.LineItemsJson);
            cmd.ExecuteNonQuery();
        }

        // ── Suppliers ─────────────────────────────────────────────────────────────

        public static List<Supplier> GetSuppliers()
        {
            var list = new List<Supplier>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name FROM Suppliers ORDER BY Name";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Supplier { Id = r.GetInt32(0), Name = r.GetString(1) });
            return list;
        }

        public static void AddSupplier(string name)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Suppliers (Name) VALUES ($n)";
            cmd.Parameters.AddWithValue("$n", name);
            cmd.ExecuteNonQuery();
        }

        // ── Backup ───────────────────────────────────────────────────────────────

        public static void BackupTo(string targetPath)
        {
            File.Copy(DbPath, targetPath, overwrite: true);
        }
    }
}
