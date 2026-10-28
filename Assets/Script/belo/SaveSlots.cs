using System;
using System.Globalization;
using UnityEngine;

public static class SaveSlots
{
    public const int Count = 3;
    public static event Action LocalSaveChanged;
    private static bool applyingCloudSnapshot;

    [Serializable]
    public class CloudSnapshot
    {
        public int activeSlot;
        public int selectedCharacter;
        public string modifiedUtc;
        public string[] slots = new string[Count];
        public string[] inventories = new string[Count];
        public string[] slotModifiedUtc = new string[Count];
    }

    [Serializable] public class Record
    {
        public string scene = "Scene 2";
        public int character;
        public bool hasPosition;
        public Vector3 position;
        public string savedAt;
        public int elfQuestStage;
        // A missing health pool in older saves starts at full health.
        public int currentHealth = -1;
        public int maxHealth;
        public System.Collections.Generic.List<string> defeatedEncounters = new System.Collections.Generic.List<string>();
    }
    public static int Active
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("CodeEscapade.ActiveSlot", 0), 0, Count - 1);
        set
        {
            PlayerPrefs.SetInt("CodeEscapade.ActiveSlot", Mathf.Clamp(value, 0, Count - 1));
            PlayerPrefs.Save();
            NotifyLocalSaveChanged();
        }
    }
    static string Key(int slot) => "CodeEscapade.Slot." + slot;
    static string ModifiedKey(int slot) => "CodeEscapade.SaveModifiedUtc.Slot." + slot;
    public static string InventoryKey => "CodeEscapade.Inventory.v1.Slot." + Active;
    public static string GetSlotModifiedUtc(int slot) => slot < 0 || slot >= Count ? "" : PlayerPrefs.GetString(ModifiedKey(slot), "");

    public static void ApplyCloudSlotModifiedUtc(int slot, string modifiedUtc)
    {
        if (slot < 0 || slot >= Count || string.IsNullOrWhiteSpace(modifiedUtc)) return;
        PlayerPrefs.SetString(ModifiedKey(slot), modifiedUtc);
        PlayerPrefs.Save();
    }

    public static Record Read(int slot)
    {
        if (slot < 0 || slot >= Count || !PlayerPrefs.HasKey(Key(slot))) return null;
        try { return JsonUtility.FromJson<Record>(PlayerPrefs.GetString(Key(slot))); }
        catch (ArgumentException) { return null; }
    }
    public static void Write(Record record)
    {
        record.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        PlayerPrefs.SetString(Key(Active), JsonUtility.ToJson(record));
        PlayerPrefs.Save();
        NotifyLocalSaveChanged();
    }
    public static void BeginNew(int character)
    {
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.SetInt("SelectedCharacter", character);
        Write(new Record { character = character });
    }
    public static void Delete(int slot)
    {
        if (slot < 0 || slot >= Count) return;
        PlayerPrefs.DeleteKey(Key(slot));
        PlayerPrefs.DeleteKey("CodeEscapade.Inventory.v1.Slot." + slot);
        PlayerPrefs.Save();
        NotifyLocalSlotChanged(slot);
    }

    public static void NotifyLocalSaveChanged()
    {
        NotifyLocalSlotChanged(Active);
    }

    static void NotifyLocalSlotChanged(int slot)
    {
        if (applyingCloudSnapshot) return;

        string now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        if (slot >= 0 && slot < Count) PlayerPrefs.SetString(ModifiedKey(slot), now);
        PlayerPrefs.SetString("CodeEscapade.SaveModifiedUtc", now);
        PlayerPrefs.Save();
        LocalSaveChanged?.Invoke();
    }

    public static bool HasLocalProgress()
    {
        for (int i = 0; i < Count; i++)
            if (Read(i) != null) return true;
        return false;
    }

    public static CloudSnapshot CaptureCloudSnapshot()
    {
        var snapshot = new CloudSnapshot
        {
            activeSlot = Active,
            selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0),
            modifiedUtc = PlayerPrefs.GetString("CodeEscapade.SaveModifiedUtc", "")
        };

        for (int i = 0; i < Count; i++)
        {
            string slotKey = "CodeEscapade.Slot." + i;
            string inventoryKey = "CodeEscapade.Inventory.v1.Slot." + i;
            snapshot.slots[i] = PlayerPrefs.HasKey(slotKey) ? PlayerPrefs.GetString(slotKey) : "";
            snapshot.inventories[i] = PlayerPrefs.HasKey(inventoryKey) ? PlayerPrefs.GetString(inventoryKey) : "";
            snapshot.slotModifiedUtc[i] = PlayerPrefs.GetString(ModifiedKey(i), "");
            if (!string.IsNullOrEmpty(snapshot.slots[i]) && string.IsNullOrEmpty(snapshot.slotModifiedUtc[i]))
            {
                DateTime legacyTime;
                var record = Read(i);
                snapshot.slotModifiedUtc[i] = record != null &&
                    DateTime.TryParseExact(record.savedAt, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out legacyTime)
                    ? legacyTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                    : DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                PlayerPrefs.SetString(ModifiedKey(i), snapshot.slotModifiedUtc[i]);
            }
        }

        if (string.IsNullOrEmpty(snapshot.modifiedUtc))
        {
            snapshot.modifiedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            PlayerPrefs.SetString("CodeEscapade.SaveModifiedUtc", snapshot.modifiedUtc);
        }
        PlayerPrefs.Save();

        return snapshot;
    }

    public static CloudSnapshot MergeCloudSnapshots(CloudSnapshot local, CloudSnapshot cloud)
    {
        if (local == null) return cloud;
        if (cloud == null) return local;

        var merged = new CloudSnapshot
        {
            activeSlot = IsNewer(local.modifiedUtc, cloud.modifiedUtc) ? local.activeSlot : cloud.activeSlot,
            selectedCharacter = IsNewer(local.modifiedUtc, cloud.modifiedUtc) ? local.selectedCharacter : cloud.selectedCharacter,
            modifiedUtc = IsNewer(local.modifiedUtc, cloud.modifiedUtc) ? local.modifiedUtc : cloud.modifiedUtc
        };

        for (int i = 0; i < Count; i++)
        {
            string localSlot = GetAt(local.slots, i);
            string cloudSlot = GetAt(cloud.slots, i);
            bool chooseLocal = string.IsNullOrEmpty(cloudSlot) ||
                (!string.IsNullOrEmpty(localSlot) && IsNewer(GetAt(local.slotModifiedUtc, i), GetAt(cloud.slotModifiedUtc, i)));
            merged.slots[i] = chooseLocal ? localSlot : cloudSlot;
            merged.inventories[i] = chooseLocal ? GetAt(local.inventories, i) : GetAt(cloud.inventories, i);
            merged.slotModifiedUtc[i] = chooseLocal ? GetAt(local.slotModifiedUtc, i) : GetAt(cloud.slotModifiedUtc, i);
        }

        return merged;
    }

    static string GetAt(string[] values, int index) =>
        values != null && index < values.Length ? values[index] ?? "" : "";

    static bool IsNewer(string left, string right)
    {
        DateTime leftTime, rightTime;
        bool hasLeft = DateTime.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out leftTime);
        bool hasRight = DateTime.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out rightTime);
        if (!hasLeft) return false;
        if (!hasRight) return true;
        return leftTime.ToUniversalTime() > rightTime.ToUniversalTime();
    }

    public static void ApplyCloudSnapshot(CloudSnapshot snapshot)
    {
        if (snapshot == null) return;

        applyingCloudSnapshot = true;
        try
        {
            for (int i = 0; i < Count; i++)
            {
                string slotKey = "CodeEscapade.Slot." + i;
                string inventoryKey = "CodeEscapade.Inventory.v1.Slot." + i;
                string slot = snapshot.slots != null && i < snapshot.slots.Length ? snapshot.slots[i] : "";
                string inventory = snapshot.inventories != null && i < snapshot.inventories.Length ? snapshot.inventories[i] : "";

                if (string.IsNullOrEmpty(slot)) PlayerPrefs.DeleteKey(slotKey);
                else PlayerPrefs.SetString(slotKey, slot);
                if (string.IsNullOrEmpty(inventory)) PlayerPrefs.DeleteKey(inventoryKey);
                else PlayerPrefs.SetString(inventoryKey, inventory);

                string modified = snapshot.slotModifiedUtc != null && i < snapshot.slotModifiedUtc.Length
                    ? snapshot.slotModifiedUtc[i]
                    : "";
                if (string.IsNullOrEmpty(modified)) PlayerPrefs.DeleteKey(ModifiedKey(i));
                else PlayerPrefs.SetString(ModifiedKey(i), modified);
            }

            PlayerPrefs.SetInt("CodeEscapade.ActiveSlot", Mathf.Clamp(snapshot.activeSlot, 0, Count - 1));
            PlayerPrefs.SetInt("SelectedCharacter", snapshot.selectedCharacter);
            PlayerPrefs.SetString("CodeEscapade.SaveModifiedUtc", snapshot.modifiedUtc ?? "");
            PlayerPrefs.Save();
        }
        finally
        {
            applyingCloudSnapshot = false;
        }
    }
    public static void MigrateLegacy()
    {
        const string migrationKey = "CodeEscapade.SlotMigration.v1";
        if (PlayerPrefs.HasKey(migrationKey)) return;
        if (Read(0) == null && PlayerPrefs.HasKey("SavedScene"))
        {
            int previous = Active;
            Active = 0;
            Write(new Record { scene = PlayerPrefs.GetString("SavedScene", "Scene 2"),
                character = PlayerPrefs.GetInt("SelectedCharacter", 0), hasPosition = true,
                position = new Vector3(PlayerPrefs.GetFloat("PlayerX"), PlayerPrefs.GetFloat("PlayerY"), PlayerPrefs.GetFloat("PlayerZ")) });
            if (PlayerPrefs.HasKey("CodeEscapade.Inventory.v1"))
                PlayerPrefs.SetString(InventoryKey, PlayerPrefs.GetString("CodeEscapade.Inventory.v1"));
            Active = previous;
        }
        PlayerPrefs.SetInt(migrationKey, 1);
        PlayerPrefs.Save();
    }
}
