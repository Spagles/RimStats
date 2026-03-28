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
    public abstract class BaseData {
        public int randSeed;
        public int tick;
        
        protected BaseData(int randSeed, int tick) {
            this.randSeed = randSeed;
            this.tick = tick;
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
    public class EventData : BaseData {
        public readonly string eventType;
        public readonly string eventLabel;
        public readonly string importance;
        public readonly string details;

        public EventData(int randSeed, int tick, string eventType, string importance, string eventLabel, string details) : base(randSeed, tick) {
            this.eventLabel = eventLabel;
            this.eventType = eventType;
            this.importance = importance;
            this.details = details;
        }
    }

    public class StatsData : BaseData {
        public readonly string factionName;
        public readonly float wealth;
        public readonly int colonists;
        public readonly string timestamp;
        public StatsData(int randSeed, int tick, string factionName, float wealth, int colonists, string timestamp) : base(randSeed, tick) {
            this.factionName = factionName;
            this.wealth = wealth;
            this.colonists = colonists;
            this.timestamp = timestamp;
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

        public static void Initialize() {
            try {
                BindBinaires();

                InitializeTable<EventData>("Events");
                InitializeTable<StatsData>("Stats");

                if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] Database successfully initialized");
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Error while initializing the database. {exception.Message}");
            }
        }

        public static void InitializeTable<T>(string tableName) where T : BaseData {
            using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
                List<string> columns = new List<string>{"id INTEGER PRIMARY KEY AUTOINCREMENT"};

                foreach (FieldInfo field in fields) {
                    string columnName = field.Name;
                    string sqlType = GetSqlType(field.FieldType);

                    columns.Add($"{columnName} {sqlType}");   
                }

                command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
                
                command.ExecuteNonQuery();
            }
        }

        public static void InsertData<T>(T data, string tableName) where T : BaseData {
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
                    command.CommandText = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                    // Anti-SQL injection
                    foreach (var entry in dataDict) {
                        command.Parameters.AddWithValue("$" + entry.Key, entry.Value ?? DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Database Insert Error. {exception.Message}");
            }

            if (RimStatsMod.settings.logEnabled) Log.Message("[RimStats] Data successfully inserted");
        }

        private static string GetSqlType(Type t) {
            if (t == typeof(int) || t == typeof(long)) return "INTEGER";
            if (t == typeof(float) || t == typeof(double)) return "REAL";
            return "TEXT";
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

            if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] SQLite DLL successfully bound from: {binPath}");
        }
    }

    internal static class Win {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string libname);
    }
}