using Godot;

namespace WarehouseRunner.Core;

public static class RecordStore
{
    private const string RecordsPath = "user://records.cfg";
    private const string Section = "best_times";

    public static double? LoadBestTime(string levelId)
    {
        var records = new ConfigFile();
        if (records.Load(RecordsPath) != Error.Ok || !records.HasSectionKey(Section, levelId))
        {
            return null;
        }

        return records.GetValue(Section, levelId).AsDouble();
    }

    public static void SaveBestTime(string levelId, double seconds)
    {
        var records = new ConfigFile();
        records.Load(RecordsPath);
        records.SetValue(Section, levelId, seconds);
        records.Save(RecordsPath);
    }
}
