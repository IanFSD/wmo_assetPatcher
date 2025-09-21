using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Data.Sqlite;

namespace WMO.SQL;

class ModModel
{
    private static DataClass WMODB = new DataClass();

    private int modId;
    private string name;
    private int fileCount;
    private bool addsCustom;
    private string fileList;

    //From reading file system
    ModModel(string modDirectory)
    {
        DirectoryInfo thisDirectory = new DirectoryInfo(modDirectory);
        setName(thisDirectory.Name);
        string[] files = Directory.GetFiles(modDirectory, "*.*", SearchOption.TopDirectoryOnly);
            setFileCount(files.Length);
        string fileList = "";
        foreach (string modFile in files) { fileList += modFile; }
            setFileList(fileList);
        setAddsCustom(false); //TO DO: Add logic for this
    }

    //From SQL
    ModModel(SqliteDataReader sql)
    {
        //assume we start with modId and move on
        setModId(sql.GetInt32(0));
        setName(sql.GetString(1));
        setFileCount(sql.GetInt32(2)); //no auto long conversion, huh
        setAddsCustom(sql.GetBoolean(3));
        setFileList(sql.GetString(4));
    }

    //CRUD OPERATIONS
        //Create insert Mod query, assuming first insert assetID will auto-gen
    public string insertModSQL(ModModel mod) {
        string insertModQuery = @"INSERT INTO Mods(name, fileCount, addsCustom, fileList)
            VALUES('" + mod.getName() + "', " + mod.getFileCount() + ", " + mod.getAddsCustom() + ", '"
            + mod.getFileList() + ")";
        return insertModQuery;
    }

        //Select Statements
    public string selectModsByName(string inputName)
    {
        string selectAssetQuery = @"SELECT * FROM Mods WHERE name =" + inputName;
        return selectAssetQuery;
    }
    public string selectModByModId(int inputId)
    {
        string selectAssetQuery = @"SELECT * FROM Mods WHERE modId =" + inputId.ToString();
        return selectAssetQuery;
    }

        //Update Asset with whole Mod Object, assuming ID or custom logic are not being manipulated directly
    public string updateMod(ModModel inputAsset)
    {
        string updateModQuery = @"UPDATE Mods set 
        name =" + inputAsset.getName() +
        " fileCount =" + inputAsset.getFileCount().ToString() +
        " fileList = " + inputAsset.getFileList();
        return updateModQuery;
    }

        //Delete Statements
    public string deleteModByName(string inputName)
    {
        string deleteAssetQuery = @"DELETE FROM Mods WHERE name =" + inputName;
        return deleteAssetQuery;
    }
    public string deleteAssetByModId(int inputId)
    {
        string deleteAssetQuery = @"DELETE FROM Mods WHERE modId =" + inputId.ToString();
        return deleteAssetQuery;
    }


    //GETTERS AND SETTERS
    public int getModId() { return modId; }
    public void setModId(int input) { modId = input; }
    public string getName() { return name; }
    public void setName(string input) { name = input; }
    public long getFileCount() { return fileCount; }
    public void setFileCount(int input) { fileCount = input; }
    public bool getAddsCustom() { return addsCustom; }
    public void setAddsCustom(bool input) { addsCustom = input; }
    public string getFileList() { return fileList; }
    public void setFileList(string input) { fileList = input; }
}
