using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Normal enemies stay cleared for a visit; elite/boss victories and reward receipts are permanent.</summary>
public static class SceneVisitProgress
{
    private static string previousScene;

    public static bool SupportsScene(string scene) => scene == "Shadowroot Depths" || scene == "Frostbound Passage";
    public static bool SupportsRespawnScene(string scene)=>EnemyMapBalance.MapIndex(scene)>=0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        previousScene = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static bool StartsNewVisit(string previous, string next) => SupportsRespawnScene(next) &&
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
        if(saved==null)return;
        scene=MapSceneNames.Resolve(scene);
        if(scene=="Whispering Hollow")saved.scene2VisitDefeated=new List<string>();
        else if(scene=="Forest of Dawn")saved.map5VisitDefeated=new List<string>();
        if (scene == "Shadowroot Depths") saved.scene3VisitDefeated = new List<string>();
        else if (scene == "Frostbound Passage") saved.map4VisitDefeated = new List<string>();
    }

    public static bool RecordDefeat(SaveSlots.Record saved, string scene, string encounterId)
    {
        scene=MapSceneNames.Resolve(scene);
        if (saved == null || !SupportsRespawnScene(scene) || string.IsNullOrWhiteSpace(encounterId)) return false;
        if (Defeated(saved, scene) == null) ResetVisit(saved, scene);
        var defeated = Defeated(saved, scene);
        if (defeated.Contains(encounterId)) return false;
        defeated.Add(encounterId);
        return true;
    }

    private static List<string> Defeated(SaveSlots.Record saved, string scene) => scene == "Shadowroot Depths"
        ? saved?.scene3VisitDefeated : scene == "Frostbound Passage" ? saved?.map4VisitDefeated : scene=="Whispering Hollow"?saved?.scene2VisitDefeated:scene=="Forest of Dawn"?saved?.map5VisitDefeated:saved?.defeatedEncounters;

    public static int Count(SaveSlots.Record saved, string scene)
    {
        var defeated = Defeated(saved, scene);
        return defeated == null ? 0 : new HashSet<string>(defeated.FindAll(id => !string.IsNullOrWhiteSpace(id))).Count;
    }

    public static bool IsDefeated(SaveSlots.Record saved, string scene, string encounterId,bool permanent=false) =>
        (permanent?saved?.defeatedEncounters:Defeated(saved, MapSceneNames.Resolve(scene)))?.Contains(encounterId) == true;
}
