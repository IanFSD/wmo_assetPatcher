using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Data.Sqlite;
using Mono.Cecil.Cil;
using SharpCompress;
using WMO.Helper;
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
                
            using var command = new SqliteCommand(query, connection); //establish command query + DB
            command.ExecuteNonQuery(); //run the query now
            connection.Close(); //close the channel
        }
        catch (Exception ex)
        {
                //log the query
            Logger.Log(LogLevel.Debug, $"=== WMO SQL Query Opened ===");
            Logger.Log(LogLevel.Debug, $"{query}");
            Logger.Log(LogLevel.Debug, $"=== End of Query ===");
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
        try{
            connection.Open();
            String dropFiles = @"DROP Table Files";
            query(dropFiles);
            String dropMods = @"DROP Table Mods";
            query(dropMods);
            String dropAssets = @"DROP TABLE Assets";
            query(dropAssets);
        }
        catch (Exception ex){
            handleSQLError(ex);
        }
    }

    /**
    * Generic asset insert query
    */
    public void insertAsset(AssetFileInfo assetFile, AssetTypeValueField assetField){
        //assetId autoincrements
        string name = assetField["m_Name"].AsString;
        long pathId = assetFile.PathId;
        int classId = assetFile.TypeId;
        string json = readJSONAsset(assetField, "");
        int lastChanged = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();

        String insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged)
                VALUES('" + name + "', " + pathId + ", " + classId + ", '" + json + "', " + lastChanged +
            ")";
        query(insertAssetQuery);
    }
    public void loadAllAssets(AssetsManager manager, AssetsFileInstance anInstance)
    {
        int[] listOfTypes = [ //list of all files we are interested in
            (int)AssetClassID.AudioClip,
            //(int)AssetClassID.Sprite,
            (int)AssetClassID.Texture2D
        ];

        var afile = anInstance.file;

        foreach (int type in listOfTypes)
        { //iterate over file types
            var files = afile.GetAssetsOfType(type); //generate file list
            if (!files.Any()){continue;} //check if list is empty, if so, move onto the next cycle
            var count = 0; //count number of rows before risking memory limit
            String insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged) VALUES";
            foreach (var assetFile in files)
            { //iterate over files in that list
                var assetField = manager.GetBaseField(anInstance, assetFile);
                //assetId autoincrements
                string name = assetField["m_Name"].AsString; long pathId = assetFile.PathId;
                int classId = assetFile.TypeId; int lastChanged = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
                string json = readJSONAsset(assetField, "");
                insertAssetQuery += " ('" + name + "', " + pathId + ", " + classId + ", '" + json + "', " + lastChanged + "),";
                count++;
                if (count > 10)
                { //memory limit, need to submit the query and then start from 0 rows
                    Logger.Log(LogLevel.Info, $"===DUMPING===");
                    insertAssetQuery = insertAssetQuery.Substring(0, insertAssetQuery.Length - 1);
                    query(insertAssetQuery);
                    count = 0;
                    insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged) VALUES";
                }
            }
            insertAssetQuery = insertAssetQuery.Substring(0, insertAssetQuery.Length-1);
            query(insertAssetQuery);
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

    /**
    * Read all fields in an AssetTypeValueField object for its json
    */
    public string readJSONAsset(AssetTypeValueField assetField, string returnString)
    {
        returnString += "{"; //open the object up in json
        foreach (AssetTypeValueField child in assetField.Children){ //loop over children
            returnString += "\"" + child.TemplateField.Name.ToString() + "\": "; //add the name
            try{ //will fail if object
                returnString += assetField[child.TemplateField.Name.ToString()].AsString + ",";} //get the value by referring the name
            catch (Exception) { returnString += readJSONAsset(child, returnString) + ","; } //if error, means we need to go deeper
        }
        returnString = returnString.Substring(0, returnString.Length-1); //if we are at the first layer, cut off the last comma
        return returnString + "}";
    }
}

