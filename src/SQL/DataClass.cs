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
            String assetsTable = @"CREATE TABLE Assets(
                assetId INT PrimaryKey,
                name TEXT NOT NULL DEFAULT a,
                pathId INT NOT NULL DEFAULT 0,
                classId INT NOT NULL DEFAULT 0,
                source TEXT NOT NULL DEFAULT a,
                lastChanged INT NOT NULL DEFAULT 0,
                modded BOOLEAN NOT NULL DEFAULT false
                )"; //note that source is the JSON
            query(assetsTable);

            //Create Table to hold meta data of mods as they are at higher level folders
            String moddedTable = @"CREATE TABLE Mods(
                modId INT PrimaryKey,
                name TEXT NOT NULL DEFAULT a,
                fileCount INT NOT NULL DEFAULT 0,
                addsCustom BOOLEAN NOT NULL DEFAULT false,
                fileList TEXT NOT NULL DEFAULT a
                )"; //addsCustom checks if it is making additional files in the system, file list is just comma array of file names
            query(moddedTable);

            //Create Table to hold individual file data of mods
            String fileTable = @"CREATE TABLE Files(
                fileId INT PrimaryKey,
                name TEXT NOT NULL DEFAULT a,
                modId INT NOT NULL,
                replacedAssetId INT,
                replacedAssetName TEXT,
                pathId INT,
                classId INT NOT NULL DEFAULT 0,
                source TEXT NOT NULL DEFAULT a,
                FOREIGN KEY (replacedAssetId) REFERENCES Assets(assetId),
                FOREIGN KEY (modId) REFERENCES Mods(modId)
                )"; //modID to connect to mod table key, and assetid/nametid/classid/source all ought to match reasonably to asset table
            query(fileTable);
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
        Logger.Log(LogLevel.Error, $"{ex.Message} \n {ex.StackTrace}");
        Logger.Log(LogLevel.Error, $"=== End of SQL Error ===");
    }
}

