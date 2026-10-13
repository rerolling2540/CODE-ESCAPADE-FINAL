using System;
using UnityEngine;

public static class SaveSlots
{
    public const int Count = 3;
    [Serializable] public class Record
    {
        public string scene = "Scene 2";
        public int character;
        public bool hasPosition;
        public Vector3 position;
        public string savedAt;
        public System.Collections.Generic.List<string> defeatedEncounters = new System.Collections.Generic.List<string>();
    }
    public static int Active
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("CodeEscapade.ActiveSlot", 0), 0, Count - 1);
        set { PlayerPrefs.SetInt("CodeEscapade.ActiveSlot", Mathf.Clamp(value, 0, Count - 1)); PlayerPrefs.Save(); }
    }
    static string Key(int slot) => "CodeEscapade.Slot." + slot;
    public static string InventoryKey => "CodeEscapade.Inventory.v1.Slot." + Active;

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
