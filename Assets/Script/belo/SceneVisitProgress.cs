using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Portal kills in Scene3 and map 4 belong to a visit; reward receipts remain permanent.</summary>
public static class SceneVisitProgress
{
    private static string previousScene;

    public static bool SupportsScene(string scene) => scene == "Shadowroot Depths" || scene == "Frostbound Passage";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        previousScene = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static bool StartsNewVisit(string previous, string next) => SupportsScene(next) &&
        previous != next && previous != "Battle Scene";

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (StartsNewVisit(previousScene, scene.name))
        {
            var saved = SaveSlots.Read(SaveSlots.Active);
            if (saved != null)
            {
                ResetVisit(saved, scene.name);
                SaveSlots.Write(saved);
            }
        }
        previousScene = scene.name;
    }

    public static void ResetVisit(SaveSlots.Record saved, string scene)
    {
        if (scene == "Shadowroot Depths") saved.scene3VisitDefeated = new List<string>();
        else if (scene == "Frostbound Passage") saved.map4VisitDefeated = new List<string>();
    }

    public static bool RecordDefeat(SaveSlots.Record saved, string scene, string encounterId)
    {
        if (saved == null || !SupportsScene(scene) || string.IsNullOrWhiteSpace(encounterId)) return false;
        if (Defeated(saved, scene) == null) ResetVisit(saved, scene);
        var defeated = Defeated(saved, scene);
        if (defeated.Contains(encounterId)) return false;
        defeated.Add(encounterId);
        return true;
    }

    private static List<string> Defeated(SaveSlots.Record saved, string scene) => scene == "Shadowroot Depths"
        ? saved?.scene3VisitDefeated : scene == "Frostbound Passage" ? saved?.map4VisitDefeated : saved?.defeatedEncounters;

    public static int Count(SaveSlots.Record saved, string scene)
    {
        var defeated = Defeated(saved, scene);
        return defeated == null ? 0 : new HashSet<string>(defeated.FindAll(id => !string.IsNullOrWhiteSpace(id))).Count;
    }

    public static bool IsDefeated(SaveSlots.Record saved, string scene, string encounterId) =>
        Defeated(saved, scene)?.Contains(encounterId) == true;
}
