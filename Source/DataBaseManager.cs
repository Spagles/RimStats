using Microsoft.Data.Sqlite;
using Verse;
using System;
using System.IO;
using System.Runtime.InteropServices;
using SQLitePCL;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Data;
using RimWorld;

// Path to database
// /home/progme/.portproton/data/prefixes/DOTNET/drive_c/users/steamuser/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimStats

namespace RimStats {
    [StaticConstructorOnStartup]
    public static class DataBaseManager {
        public static readonly string directory = Path.Combine(GenFilePaths.ConfigFolderPath, "RimStats");
        public static readonly string path = Path.Combine(directory, "Data.db");
        public static readonly string connectionString;
        private static readonly Dictionary<Type, string> tableRegistry = new Dictionary<Type, string> {
            {typeof(EventData), "Events"},
            {typeof(StatsData), "Stats"}
        };

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
                Log.Error($"{RimStatsMod.Prefix} Databse init failed : {exception.Message}");
            }
        }

        public static void Initialize() {
            try {
                BindBinaires();

                InitializeTable<EventData>();
                InitializeTable<StatsData>();

                if (RimStatsMod.settings.logEnabled) Log.Message($"{RimStatsMod.Prefix} Database successfully initialized");
            }
            catch (Exception exception) {
                Log.Error($"{RimStatsMod.Prefix} Error while initializing the database. {exception.Message}");
            }
        }

        public static void InitializeTable<T>() where T : BaseData {
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

                command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableRegistry[typeof(T)]} ({string.Join(", ", columns)})";
                
                command.ExecuteNonQuery();
            }
        }

        public static List<T> ExtractData<T>(int randSeed) where T : BaseData {
            List<T> dataList = new List<T>();
            
            if (!tableRegistry.TryGetValue(typeof(T), out string tableName)) {
                throw new KeyNotFoundException($"Type {typeof(T).Name} is not registered in tableRegistry.");
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                connection.Open();

                using (SqliteCommand command = connection.CreateCommand()) {
                    command.CommandText = $"SELECT * FROM {tableName} WHERE randSeed = @seed ORDER BY tick ASC";
                    command.Parameters.AddWithValue("@seed", randSeed);

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            T item = MapRowToType<T>(reader);
                            dataList.Add(item);
                        }
                    }
                }
            }
            return dataList;
        }

        private static T MapRowToType<T>(IDataReader reader) where T : BaseData {
            var constructor = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            
            var parametersInfo = constructor.GetParameters();
            object[] constructorArgs = new object[parametersInfo.Length];

            for (int i = 0; i < parametersInfo.Length; i++) {
                string paramName = parametersInfo[i].Name;
                
                int ordinal = reader.GetOrdinal(paramName);

                if (ordinal != -1 && !reader.IsDBNull(ordinal)) {
                    object rawValue = reader.GetValue(ordinal);
                    constructorArgs[i] = Convert.ChangeType(rawValue, parametersInfo[i].ParameterType);
                } else {
                    constructorArgs[i] = parametersInfo[i].ParameterType.IsValueType 
                                        ? Activator.CreateInstance(parametersInfo[i].ParameterType) 
                                        : null;
                }
            }
            
            return (T)constructor.Invoke(constructorArgs);
        }

        public static bool InsertData<T>(T data) where T : BaseData {
            bool success = false;
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
                    command.CommandText = $"INSERT INTO {tableRegistry[typeof(T)]} ({columns}) VALUES ({values})";

                    // Anti-SQL injection
                    foreach (var entry in dataDict) {
                        command.Parameters.AddWithValue("$" + entry.Key, entry.Value ?? DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                    success = true;
                }
            }
            catch (Exception exception) {
                Log.Error($"{RimStatsMod.Prefix} Error inserting into {tableRegistry[typeof(T)]}. {exception.Message}");
            }

            return success;
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

            if (RimStatsMod.settings.logEnabled) Log.Message($"{RimStatsMod.Prefix} SQLite DLL successfully bound from: {binPath}");
        }
    }

    internal static class Win {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string libname);
    }
}