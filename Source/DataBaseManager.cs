using Microsoft.Data.Sqlite;
using Verse;
using System;
using System.IO;
using System.Runtime.InteropServices;
using SQLitePCL;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

// Path to database
// /home/progme/.portproton/data/prefixes/DOTNET/drive_c/users/steamuser/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimStats

namespace RimStats {
    public class StatsData {
        public readonly int randSeed;
        public readonly string factionName;
        public readonly float wealth;
        public readonly int colonists;
        public readonly int tick;
        public readonly string timestamp;
        public StatsData(int randSeed, string factionName, float wealth, int colonists, int tick, string timestamp) {
            this.factionName = factionName;
            this.randSeed = randSeed;
            this.wealth = wealth;
            this.colonists = colonists;
            this.tick = tick;
            this.timestamp = timestamp;
        }

        public Dictionary<string, object> ToDictionary() {
            var dict = new Dictionary<string, object>();
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields) {
                dict.Add(field.Name, field.GetValue(this));
            }

            return dict;
        }
    }

    [StaticConstructorOnStartup]
    public static class DataBaseManager {
        public static readonly string directory = Path.Combine(GenFilePaths.ConfigFolderPath, "RimStats");
        public static readonly string path = Path.Combine(directory, "Data.db");
        public static readonly string connectionString;

        static DataBaseManager() {
            // Create directory if it doesn't exist
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // Create connection builder to get string
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };

            connectionString = builder.ConnectionString;

            try {
                BindBinaires();
                Initialize();
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Databse init failed : {exception.Message}");
            }
        }

        private static void BindBinaires() {
            ModMetaData mod = ModLister.GetModWithIdentifier("progme.rimstats");
            if (mod == null) return;

            string binPath = Path.Combine(mod.RootDir.ToString(), "Bin");

            Batteries_V2.Init();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                string dllPath = Path.Combine(binPath, "e_sqlite3.dll");
                if (File.Exists(dllPath)) Win.LoadLibrary(dllPath);
            }

            raw.SetProvider(new SQLite3Provider_e_sqlite3());
            Log.Message($"[RimStats] SQLite DLL successfully bound from: {binPath}");
        }

        public static void Initialize() {
            try {
                using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                    connection.Open();
                    SqliteCommand command = connection.CreateCommand();

                    command.CommandText = GetCreateTableQuery<StatsData>(tableName : "Stats");
                    
                    command.ExecuteNonQuery();
                }
            Log.Message($"[RimStats] Database initialized at {path}");
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Error while initializing the database. {exception.Message}");
            }
        }

        public static string GetCreateTableQuery<DataType>(string tableName) where DataType : class {
            FieldInfo[] fields = typeof(DataType).GetFields(BindingFlags.Public | BindingFlags.Instance);
            List<string> columns = new List<string>{"id INTEGER PRIMARY KEY AUTOINCREMENT"};

            foreach (FieldInfo field in fields) {
                string columnName = field.Name;
                string sqlType = "TEXT";

                switch (field.FieldType) {
                    case Type t when t == typeof(int) || t == typeof(long) || t == typeof(short):
                        sqlType = "INTEGER";
                        break;
                    case Type t when t == typeof(float) || t == typeof(double) || t == typeof(decimal):
                        sqlType = "REAL";
                        break;
                    case Type t when t == typeof(bool):
                        sqlType = "INTEGER";
                        break;
                }

                columns.Add($"{columnName} {sqlType}");   
            }

            string query = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
            return query;
        }

        public static void InsertData(StatsData data) {
            try {
                // Create directory if it doesn't exist
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                    connection.Open();
                    SqliteCommand command = connection.CreateCommand();

                    Dictionary<string, object> dataDict = data.ToDictionary();

                    string columns = string.Join(", ", dataDict.Keys);
                    string values = string.Join(", ", dataDict.Keys.Select(k => "$" + k));

                    // Add text to command
                    command.CommandText = $"INSERT INTO Stats ({columns}) VALUES ({values})";

                    // Anti-SQL injection
                    foreach (var entry in dataDict) {
                        command.Parameters.AddWithValue("$" + entry.Key, entry.Value ?? DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Database Insert Error : {exception.Message}");
            }

            Log.Message("[RimStats] Data successfully inserted");
        }
    }

    internal static class Win {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string libname);
    }
}