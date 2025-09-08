using WMO.SQL;

internal static class Program2
{
    private static void Main(string[] args)
    {
        DataClass thisDB = new DataClass();
        thisDB.createTables();

        //NEXT, read asset rip and insert into assets folder
        //THEN, read mod data and insert it into mods folder
        //FINALLY, create more specific CRUD options and move query to private
        //POST, ensure data persists between sessions & create "wipe" which will allow user to rebuild from scratch
    }
}