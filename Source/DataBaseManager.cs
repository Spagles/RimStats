using Microsoft.Data.Sqlite;
using Verse;
using System;
using System.IO;
using System.Runtime.InteropServices;
using SQLitePCL;

// Path to database
// /home/progme/.portproton/data/prefixes/DOTNET/drive_c/users/steamuser/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimStats

namespace RimStats {
    [StaticConstructorOnStartup]
    public static class DataBaseManager {
        private static readonly string directory = Path.Combine(GenFilePaths.ConfigFolderPath, "RimStats");
        private static readonly string path = Path.Combine(directory, "Data.db");
        private static readonly string connectionString;

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
                Log.Error($"[RimStats] Init failed : {exception.Message}");
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

                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS GameEvents (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        EventType TEXT,
                        Message TEXT
                    );";

                    command.ExecuteNonQuery();
                }
            Log.Message($"[RimStats] Database initialized at {path}");
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Error while initializing the database. {exception.Message}");
            }
        }

        public static void InsertEvent(string type, string message) {
            try {
                // Create directory if it doesn't exist
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                    connection.Open();
                    SqliteCommand command = connection.CreateCommand();

                    // Add text to command
                    command.CommandText = "INSERT INTO GameEvents (EventType, Message) VALUES ($type, $message)";

                    // Anti-SQL injection
                    command.Parameters.AddWithValue("$type", type);
                    command.Parameters.AddWithValue("$message", message);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exception) {
                Log.Error($"[RimStats] Error while writing to database. {exception.Message}");
            }
        }
    }

    internal static class Win {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string libname);
    }
}