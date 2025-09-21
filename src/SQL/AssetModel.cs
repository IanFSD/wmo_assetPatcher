using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Data.Sqlite;

namespace WMO.SQL;

class AssetModel
{
    private static DataClass WMODB = new DataClass();

    private int assetID;
    private string name;
    private long pathId;
    private int classId;
    private string json;
    private int lastChanged;
    private bool modded;

    //Setting Asset Object from reading the assets
    public AssetModel(AssetFileInfo assetFile, AssetTypeValueField assetField){
        setName(assetField["m_Name"].AsString);
        setPathId(assetFile.PathId);
        setClassId(assetFile.TypeId);
        setJson(readJSONAsset(assetField, ""));
        setLastChanged((int)DateTimeOffset.Now.ToUnixTimeMilliseconds());
    }

    //Setting Asset Object from reading the Database
    public AssetModel(SqliteDataReader sql)
    {
            //assume we start with assetID and move on
        setAssetId(sql.GetInt32(0)); 
        setName(sql.GetString(1));
        setPathId(long.Parse(sql.GetString(2))); //no auto long conversion, huh
        setClassId(sql.GetInt32(3));
        setJson(sql.GetString(4));
        setLastChanged(sql.GetInt32(5));
        setModded(sql.GetBoolean(6));
    }

    //An empty AssetModel, if we just need to access a more complex query
    public AssetModel(){}

    /**
    * Read all fields in an AssetTypeValueField object for its json
    *   assetField = The specific field we are reading which contains JSON information
    *   returnString = a generic string value, it is possible for this process to loop into itself with nested objects, 
    * this lets us continue the loop.
    */
    public string readJSONAsset(AssetTypeValueField assetField, string returnString)
    {
        returnString += "{"; //open the object up in json
        foreach (AssetTypeValueField child in assetField.Children)
        { //loop over children
            returnString += "\"" + child.TemplateField.Name.ToString() + "\": "; //add the name
            try
            { //will fail if object
                returnString += assetField[child.TemplateField.Name.ToString()].AsString + ",";
            } //get the value by referring the name
            catch (Exception) { returnString += readJSONAsset(child, returnString) + ","; } //if error, means we need to go deeper
        }
        returnString = returnString.Substring(0, returnString.Length - 1); //if we are at the first layer, cut off the last comma
        return returnString + "}";
    }

    //CRUD OPERATIONS
        //Create insertAsset query, assuming first insert so modded can default to false (0) and assetID will auto-gen
    public string insertAssetSQL(AssetModel asset) {
        string insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged)
            VALUES('" + asset.getName() + "', " + asset.getPathId() + ", " + asset.getClassId() + ", '"
            + asset.getJson() + "', " + asset.getLastChanged() + ")";
        return insertAssetQuery;
    }

    //A special load/insert which loops through all files presented to it and inserts in increments of 10 assets
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
            if (!files.Any()) { continue; } //check if list is empty, if so, move onto the next cycle
            var count = 0; //count number of rows before risking memory limit
            String insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged) VALUES";
            foreach (var assetFile in files)
            { //iterate over files in that list
                var assetField = manager.GetBaseField(anInstance, assetFile);
                AssetModel thisAsset = new AssetModel(assetFile, assetField);
                insertAssetQuery += " ('" + thisAsset.getName() + "', " + thisAsset.getPathId() + ", "
                + thisAsset.getClassId() + ", '" + thisAsset.getJson() + "', " + thisAsset.getLastChanged() + "),";
                count++;
                if (count > 10)
                { //memory limit, need to submit the query and then start from 0 rows
                    //Logger.Log(LogLevel.Info, $"===DUMPING===");
                    insertAssetQuery = insertAssetQuery.Substring(0, insertAssetQuery.Length - 1);
                    WMODB.query(insertAssetQuery);
                    count = 0;
                    insertAssetQuery = @"INSERT INTO Assets(name, pathId, classId, source, lastChanged) VALUES";
                }
            }
            insertAssetQuery = insertAssetQuery.Substring(0, insertAssetQuery.Length - 1);
            WMODB.query(insertAssetQuery);
        }
    }



    //Select Statements
    public string selectAssetByName(string inputName)
    {
        string selectAssetQuery = @"SELECT * FROM Assets WHERE name =" + inputName;
        return selectAssetQuery;
    }
    public string selectAssetByPathId(int inputId)
    {
        string selectAssetQuery = @"SELECT * FROM Assets WHERE pathId =" + inputId.ToString();
        return selectAssetQuery;
    }

        //Update Asset with whole Asset Object, assuming IDs are not being manipulated directly
    public string updateAsset(AssetModel inputAsset)
    {
        string updateAssetQuery = @"UPDATE Assets set 
        name =" + inputAsset.getName() +
        " json =" + inputAsset.getJson() +
        " lastChanged = " + inputAsset.getLastChanged() +
        " modded = " + inputAsset.getModded();
        return updateAssetQuery;
    }

        //Delete Statements
    public string deleteAssetByName(string inputName)
    {
        string deleteAssetQuery = @"DELETE FROM Assets WHERE name =" + inputName;
        return deleteAssetQuery;
    }
    public string deleteAssetByPathId(int inputId)
    {
        string deleteAssetQuery = @"DELETE FROM Assets WHERE pathId =" + inputId.ToString();
        return deleteAssetQuery;
    }
    public string deleteAssetByAssetId(int inputId)
    {
        string deleteAssetQuery = @"DELETE FROM Assets WHERE assetId =" + inputId.ToString();
        return deleteAssetQuery;
    }

    //GETTERS AND SETTERS
    public int getAssetId() { return assetID; }
    public void setAssetId(int input) { assetID = input; }
    public string getName() { return name; }
    public void setName(string input) { name = input; }
    public long getPathId() { return pathId; }
    public void setPathId(long input) { pathId = input; }
    public int getClassId() { return classId; }
    public void setClassId(int input) { classId = input; }
    public string getJson() { return json; }
    public void setJson(string input) { name = json; }
    public int getLastChanged() { return lastChanged; }
    public void setLastChanged(int input) { lastChanged = input; }
    public bool getModded() { return modded; }
    public void setModded(bool input) { modded = input; }
}