using System;

// Compile against the current Unity gameplay assembly; MergeCloudSnapshots itself
// is pure managed code, so these checks don't need to open scenes or alter saves.
public static class SaveMergeRegression
{
    static int checks;
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        checks++;
    }
    static SaveSlots.CloudSnapshot Snapshot(int slot, string time, string value, string inventory = "")
    {
        var result = new SaveSlots.CloudSnapshot { modifiedUtc = time };
        result.slots[slot] = value;
        result.inventories[slot] = inventory;
        result.slotModifiedUtc[slot] = time;
        return result;
    }
    public static void Main()
    {
        const string oldTime = "2026-11-06T01:00:00Z", newTime = "2026-11-06T02:00:00Z";
        var oldSave = Snapshot(0,oldTime,"old","old items");
        var deleted = Snapshot(0,newTime,"");
        var merged = SaveSlots.MergeCloudSnapshots(deleted,oldSave);
        Check(merged.slots[0] == "", "Local deletion must beat older cloud save");
        Check(merged.inventories[0] == "", "Deleted slot must not retain old inventory");
        Check(merged.slotModifiedUtc[0] == newTime, "Deletion timestamp must survive merge");
        merged = SaveSlots.MergeCloudSnapshots(oldSave,deleted);
        Check(merged.slots[0] == "", "Cloud deletion must beat older local save");
        Check(merged.inventories[0] == "", "Cloud deletion must remove the local inventory");
        var recreated = Snapshot(0,newTime,"recreated","new items");
        merged = SaveSlots.MergeCloudSnapshots(recreated,Snapshot(0,oldTime,""));
        Check(merged.slots[0] == "recreated", "New save must beat an older deletion");
        Check(merged.inventories[0] == "new items", "Winning save keeps its matching inventory");
        merged = SaveSlots.MergeCloudSnapshots(recreated,deleted);
        Check(merged.slots[0] == "", "Timestamp tie must match server rule: cloud wins");
        var local = Snapshot(1,newTime,"local slot");
        merged = SaveSlots.MergeCloudSnapshots(local,oldSave);
        Check(merged.slots[0] == "old" && merged.slots[1] == "local slot", "Independent slots must merge");
        Check(oldSave.slots[0] == "old" && local.slots[0] == null, "Input snapshots must remain unchanged");
        merged = SaveSlots.MergeCloudSnapshots(Snapshot(0,null,"legacy"),new SaveSlots.CloudSnapshot());
        Check(merged.slots[0] == "legacy", "Untimestamped legacy saves remain readable");
        merged = SaveSlots.MergeCloudSnapshots(Snapshot(0,null,"legacy"),deleted);
        Check(merged.slots[0] == "", "Timestamped deletion must beat legacy data");
        var last = Snapshot(9,newTime,"slot ten");
        merged = SaveSlots.MergeCloudSnapshots(last,oldSave);
        Check(merged.slots[9] == "slot ten", "All ten slots must merge");
        Check(SaveSlots.MergeCloudSnapshots(null,last) == last, "Missing local snapshot must preserve cloud");
        Console.WriteLine("PASS: " + checks + " client save merge checks.");
    }
}
