using WMO.SQL;

internal static class Program2
{
    private static void Main(string[] args)
    {
        DataClass thisDB = new DataClass();
        thisDB.createTables();

        //NEXT, insert "fake" file and create basic CRUD operations
        //THEN, read asset rip and insert into assets folder
        //FINALLY, read mod data and insert it into mods folder
        //POST, ensure data persists between sessions
    }
}