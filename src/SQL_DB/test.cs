using Microsoft.Data.Sqlite;
using SQLitePCL;
using WMO.Logging;

namespace WMO.SQL;

class DataClass
    {

/**
* For testing only, will remove Main once SQL is working as intended
**/
    private static void Main(string[] args)
    {
        query("lol");
    }

public static void query(string query)
    {
        try
        {
            using var connection = new SqliteConnection(@"Data Source=../testdb.db");
            connection.Open();
            Logger.Log(LogLevel.Info, $"=== WMO SQL Query Opened ===");
            Logger.Log(LogLevel.Info, $"{query}");
            Logger.Log(LogLevel.Info, $"=== End of Query ===");

            using var command = new SqliteCommand(query, connection);
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            Logger.Log(LogLevel.Error, $"=== WMO SQL Query Failed ===");
            Logger.Log(LogLevel.Error, $"Code {ex.ErrorCode}");
            Logger.Log(LogLevel.Error, $"{ex.Message} \n {ex.StackTrace}");
            Logger.Log(LogLevel.Error, $"=== End of SQL Error ===");
        }
    }
}

