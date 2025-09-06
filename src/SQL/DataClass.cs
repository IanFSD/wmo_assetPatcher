using Microsoft.Data.Sqlite;
using WMO.Logging;

namespace WMO.SQL;

class DataClass
{
    private SqliteConnection connection; //private, don't want external code opening DB connections

    public DataClass()
    {
        connection = new SqliteConnection(@"Data Source=../WMO.db");
            //all code will use WMO.db in debug folder
            //if no file, the connection will create one automatically
    }

    /**
    * Generic query function for using SQLite table
    */
    public void query(string query) //may change to private once more SQL commands are established
    {
        try
        {
            connection.Open(); //open channel to DB
                //log the query
            Logger.Log(LogLevel.Info, $"=== WMO SQL Query Opened ===");
            Logger.Log(LogLevel.Info, $"{query}");
            Logger.Log(LogLevel.Info, $"=== End of Query ===");

            using var command = new SqliteCommand(query, connection); //establish command query + DB
            command.ExecuteNonQuery(); //run the query now
            connection.Close(); //close the channel
        }
        catch (SqliteException ex)
        {
            handleSQLError(ex); //deal with errors
        }
    }

    /**
    * If requisite tables do not yet exist, then create them.
    * TO DO: Remove defaults except lastChanged and modded
    */
    public void createTables()
    {
        try
        {
            connection.Open(); //open DB connection
            //Create table to hold all original file content of game
            //lastChanged is INT, # of seconds since 1970 as SQLite doesn't have native datetime
            //Primary key is assetPath-name, the full filepath and name of the file must be unique
            String assetsTable = @"CREATE TABLE Assets(
                name TEXT NOT NULL,
                assetPath TEXT NOT NULL,
                pathID INT NOT NULL DEFAULT 0,
                classIDTypeNumber INT NOT NULL DEFAULT 0,
                JSON TEXT NOT NULL DEFAULT a,
                lastChanged INT NOT NULL DEFAULT 0,
                modded BOOLEAN NOT NULL DEFAULT false,
                PRIMARY KEY (assetPath, name)
                )";
            query(assetsTable);

            //Create Table to hold all modded content added to game
            String moddedTable = @"CREATE TABLE Mods(
                name TEXT NOT NULL,
                assetPath TEXT NOT NULL,
                pathID INT NOT NULL DEFAULT 0,
                classIDTypeNumber INT NOT NULL DEFAULT 0,
                JSON TEXT NOT NULL DEFAULT a,
                lastChanged INT NOT NULL DEFAULT 0,
                extensionFile TEXT NOT NULL DEFAULT a,
                PRIMARY KEY (assetPath, name)
                )";
            query(moddedTable);
        }
        catch (Exception ex)
        {
            connection.Close(); //ensure connection is closed
            Logger.Log(LogLevel.Error, $"=== WMO SQL ERROR ===");
            Logger.Log(LogLevel.Error, $"{ex.Message} \n {ex.StackTrace}"); //report error
            Logger.Log(LogLevel.Error, $"=== End of SQL Error ===");
        }
    }

    /**
    * Generic Error Logging and closing taking in SQL exception data
    */
    public void handleSQLError(SqliteException ex)
    {
        connection.Close();
        Logger.Log(LogLevel.Error, $"=== WMO SQL Query Failed ===");
        Logger.Log(LogLevel.Error, $"Code {ex.ErrorCode}");
        Logger.Log(LogLevel.Error, $"{ex.Message} \n {ex.StackTrace}");
        Logger.Log(LogLevel.Error, $"=== End of SQL Error ===");
    }
}

