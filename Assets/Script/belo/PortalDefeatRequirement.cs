using System.Collections.Generic;

public static class PortalDefeatRequirement
{
    public static int Count(SaveSlots.Record saved, bool countAllEnemies, string sceneName = null)
    {
        if (SceneVisitProgress.SupportsScene(sceneName)) return SceneVisitProgress.Count(saved, sceneName);
        if (!countAllEnemies) return ElfQuestProgress.Cleared(saved);
        var defeated = new HashSet<string>();
        if (saved?.defeatedEncounters != null)
            foreach (string id in saved.defeatedEncounters)
                if (!string.IsNullOrWhiteSpace(id)) defeated.Add(id);
        return defeated.Count;
    }
}
