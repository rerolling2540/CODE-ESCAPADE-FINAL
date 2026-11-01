using System.Collections.Generic;

public static class PortalDefeatRequirement
{
    public static int Count(SaveSlots.Record saved, bool countAllEnemies)
    {
        if (!countAllEnemies) return ElfQuestProgress.Cleared(saved);
        var defeated = new HashSet<string>();
        if (saved?.defeatedEncounters != null)
            foreach (string id in saved.defeatedEncounters)
                if (!string.IsNullOrWhiteSpace(id)) defeated.Add(id);
        return defeated.Count;
    }
}
