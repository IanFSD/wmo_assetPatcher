using System.Text.RegularExpressions;
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
    * Note: CAN RETURN NULL!!! If using Select, the calling function must be able to handle the null!
    */
    public SqliteDataReader query(string query) //may change to private once more SQL commands are established
    {
        try
        {
            connection.Open(); //open channel to DB

            using var command = new SqliteCommand(query, connection); //establish command query + DB
            if (Regex.IsMatch(query, @"\bSELECT\b"))
            { //if we are using a SELECT statement with the capital word
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read()) //NOTE!!! Only returns us 1st row at a time
                {
                    connection.Close(); //close the channel
                    return reader; //return our select search result
                } //else, there were no records and we continue to simply close
            }
            else
            { //it's not a SELECT, we move to non-return
                command.ExecuteNonQuery();
            }
            connection.Close(); //guarantee channel close ehre
            return null;
        }
        catch (Exception ex)
        {
            //log the query
            Logger.Log(LogLevel.Debug, $"=== WMO SQL Query Opened ===");
            Logger.Log(LogLevel.Debug, $"{query}");
            Logger.Log(LogLevel.Debug, $"=== End of Query ===");
            handleSQLError(ex); //deal with errors
            return null;
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
                assetId INTEGER PRIMARY KEY,
                name TEXT NOT NULL DEFAULT a,
                pathId INTEGER NOT NULL DEFAULT 0,
                classId INTEGER NOT NULL DEFAULT 0,
                source TEXT NOT NULL DEFAULT a,
                lastChanged INTEGER NOT NULL DEFAULT 0,
                modded BOOLEAN NOT NULL DEFAULT false
                )"; //note that source is the JSON
            query(assetsTable);

            //Create Table to hold meta data of mods as they are at higher level folders
            String moddedTable = @"CREATE TABLE Mods(
                modId INTEGER PRIMARY KEY,
                name TEXT NOT NULL DEFAULT a,
                fileCount INTEGER NOT NULL DEFAULT 0,
                addsCustom BOOLEAN NOT NULL DEFAULT false,
                fileList TEXT NOT NULL DEFAULT a
                )"; //addsCustom checks if it is making additional files in the system, file list is just comma array of file names
            query(moddedTable);

            //Create Table to hold individual file data of mods
            String fileTable = @"CREATE TABLE Files(
                fileId INTEGER PRIMARY KEY,
                name TEXT NOT NULL DEFAULT a,
                modId INTEGER NOT NULL,
                replacedAssetId INT,
                replacedAssetName TEXT,
                pathId INTEGER,
                classId INTEGER NOT NULL DEFAULT 0,
                source TEXT NOT NULL DEFAULT a,
                FOREIGN KEY (replacedAssetId) REFERENCES Assets(assetId),
                FOREIGN KEY (modId) REFERENCES Mods(modId)
                )"; //modID to connect to mod table key, and assetid/nametid/classid/source all ought to match reasonably to asset table
            query(fileTable);
        }
        catch (Exception ex)
        {
            handleSQLError(ex);
        }
    }

    /**
    * Drop all tables to restart the DB.
    * TO DO: Probably want this to just throw error and kill entire process if this happens...
    */
    public void dropTables()
    {
        try
        {
            connection.Open();
            String dropFiles = @"DROP Table Files";
            query(dropFiles);
            String dropMods = @"DROP Table Mods";
            query(dropMods);
            String dropAssets = @"DROP TABLE Assets";
            query(dropAssets);
        }
        catch (Exception ex)
        {
            handleSQLError(ex);
        }
    }

    /**
    * Generic Error Logging and closing taking in SQL exception data
    */
    public void handleSQLError(Exception ex)
    {
        connection.Close();
        Logger.Log(LogLevel.Error, $"=== WMO SQL Query Failed ===");
        Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
        Logger.Log(LogLevel.Error, $"{ex.Message} \n {ex.StackTrace}");
        Logger.Log(LogLevel.Error, $"=== End of SQL Error ===");
    }
}