using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Map discovery belongs to a save slot. Old saves retain their known save/checkpoint maps.</summary>
public static class WorldTravelDiscovery
{
    public static string Canonical(string scene)=>MapSceneNames.Resolve(scene);
    // The grove has no exploration entrance; the elf unlocks its first monolith journey.
    public static bool CanTravelTo(SaveSlots.Record save,string scene)=>scene=="MainMenu"||HasVisited(save,scene)||
        Canonical(scene)=="Forest of Dawn"&&save!=null&&save.wardenAccepted&&WardenQuest.Chapter(save)>=5;
    public static bool HasVisited(SaveSlots.Record save,string scene)
    {
        scene=Canonical(scene);
        if(save==null||!CharacterAlmanac.IsExplorationScene(scene))return false;
        return Canonical(save.scene)==scene||save.hasCheckpoint&&Canonical(save.checkpointScene)==scene||
            save.visitedScenes!=null&&save.visitedScenes.Exists(s=>Canonical(s)==scene);
    }
    public static bool RecordVisit(SaveSlots.Record save,string scene)
    {
        scene=Canonical(scene);if(save==null||!CharacterAlmanac.IsExplorationScene(scene))return false;
        if(save.visitedScenes==null)save.visitedScenes=new List<string>();
        bool changed=false;
        foreach(var known in new[]{Canonical(save.scene),save.hasCheckpoint?Canonical(save.checkpointScene):null,scene})
            if(CharacterAlmanac.IsExplorationScene(known)&&!save.visitedScenes.Contains(known)){save.visitedScenes.Add(known);changed=true;}
        return changed;
    }
    public static void RecordCurrentScene()
    {
        var scene=SceneManager.GetActiveScene().name;if(!CharacterAlmanac.IsExplorationScene(scene))return;
        var save=SaveSlots.Read(SaveSlots.Active);if(RecordVisit(save,scene))SaveSlots.Write(save);
    }
}
