using System;
using UnityEngine;

public static class WorldTravelDiscoveryChecks
{
    public static string Run()
    {
        int count=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);count++;};
        var first=new SaveSlots.Record{scene="Whispering Hollow"};
        check(WorldTravelDiscovery.HasVisited(first,"Whispering Hollow"),"Starting map is not known");
        foreach(var map in new[]{"Shadowroot Depths","Frostbound Passage","Forest of Dawn"})check(!WorldTravelDiscovery.HasVisited(first,map),"New save unlocked "+map);
        check(WorldTravelDiscovery.RecordVisit(first,"Shadowroot Depths"),"First visit was not recorded");
        check(WorldTravelDiscovery.HasVisited(first,"Shadowroot Depths"),"Visited map remains locked");
        check(!WorldTravelDiscovery.RecordVisit(first,"Shadowroot Depths"),"Repeat visit created duplicate data");
        check(first.visitedScenes.Count==2,"Known initial map not retained");
        first.scene="Shadowroot Depths";WorldTravelDiscovery.RecordVisit(first,"Frostbound Passage");first.scene="Frostbound Passage";
        check(WorldTravelDiscovery.HasVisited(first,"Whispering Hollow")&&WorldTravelDiscovery.HasVisited(first,"Shadowroot Depths"),"Returning maps lost discovery");
        check(!WorldTravelDiscovery.HasVisited(first,"Forest of Dawn"),"Later map unlocked from earlier visit");
        var second=new SaveSlots.Record{scene="Whispering Hollow"};check(!WorldTravelDiscovery.HasVisited(second,"Frostbound Passage"),"Discovery leaked into another save");
        var loaded=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(first));check(WorldTravelDiscovery.HasVisited(loaded,"Frostbound Passage")&&WorldTravelDiscovery.HasVisited(loaded,"Shadowroot Depths"),"Discovery lost after reload");
        var old=new SaveSlots.Record{scene="Scene3",hasCheckpoint=true,checkpointScene="Scene 2",visitedScenes=null};
        check(WorldTravelDiscovery.HasVisited(old,"Shadowroot Depths")&&WorldTravelDiscovery.HasVisited(old,"Whispering Hollow"),"Old known maps not recognized");
        check(!WorldTravelDiscovery.HasVisited(old,"Frostbound Passage"),"Old save granted unproven visit");
        WorldTravelDiscovery.RecordVisit(old,"Frostbound Passage");old.scene="Frostbound Passage";old.checkpointScene="Frostbound Passage";
        check(WorldTravelDiscovery.HasVisited(old,"Shadowroot Depths")&&WorldTravelDiscovery.HasVisited(old,"Whispering Hollow"),"Migration lost old known maps");
        check(!WorldTravelDiscovery.RecordVisit(first,"Battle Scene"),"Battle scene counted as map discovery");
        check(!WorldTravelDiscovery.HasVisited(null,"Shadowroot Depths"),"Empty slot unlocked a map");
        check(WorldTravelDiscovery.HasVisited(new SaveSlots.Record{scene="Scene4"},"Frostbound Passage"),"Legacy Map 4 alias not recognized");
        check(!MonolithTravelPanel.IsDiscovered(new WorldTravelCatalog.Destination{sceneName="MainMenu",lobby=true}),"Lobby appeared in the map-only list");
        var warden=new SaveSlots.Record{scene="Frostbound Passage",wardenAccepted=true,wardenChapter=4};
        check(!WorldTravelDiscovery.CanTravelTo(warden,"Forest of Dawn"),"Final journey unlocked before Map 4 reward");
        warden.wardenChapter=5;
        check(WorldTravelDiscovery.CanTravelTo(warden,"Forest of Dawn"),"Final journey has no first-visit route");
        check(!WorldTravelDiscovery.HasVisited(warden,"Forest of Dawn"),"Quest unlock fabricated a visit");
        check(!WorldTravelDiscovery.CanTravelTo(warden,"Shadowroot Depths"),"Final journey unlocked other unvisited maps");
        var restored=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(warden));
        check(WorldTravelDiscovery.CanTravelTo(restored,"Forest of Dawn"),"Final journey lost on reload");
        restored.wardenChapter=6;check(WorldTravelDiscovery.CanTravelTo(restored,"Forest of Dawn"),"Completed campaign lost grove access");
        restored.wardenAccepted=false;check(!WorldTravelDiscovery.CanTravelTo(restored,"Forest of Dawn"),"Unaccepted quest unlocked grove");
        check(!WorldTravelDiscovery.CanTravelTo(second,"Forest of Dawn"),"Final journey unlock leaked between saves");
        check(!WorldTravelDiscovery.CanTravelTo(null,"Forest of Dawn"),"Empty slot unlocked grove");
        WorldTravelDiscovery.RecordVisit(second,"Forest of Dawn");
        check(WorldTravelDiscovery.CanTravelTo(second,"Forest of Dawn"),"Previously visited grove lost return access");
        var migration=new SaveSlots.Record{scene="map 5",checkpointScene="Scene3",position=new Vector3(12,34,0),level=7,experience=230,currentHealth=24,
            visitedScenes=new System.Collections.Generic.List<string>{"Scene3","Shadowroot Depths","map 4"},
            solvedPortalQuestions=new System.Collections.Generic.List<string>{"Assets/Scenes/Scene3.unity/portal-question/gate"}};
        migration=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(migration));
        MapSceneNames.NormalizeSave(migration);
        check(migration.scene=="Forest of Dawn"&&migration.checkpointScene=="Shadowroot Depths","Renamed save/checkpoint destination lost");
        check(migration.position==new Vector3(12,34,0)&&migration.level==7&&migration.experience==230&&migration.currentHealth==24,"Scene migration changed player progress");
        check(migration.visitedScenes.Count==2&&WorldTravelDiscovery.HasVisited(migration,"Frostbound Passage"),"Scene migration lost discovered maps");
        check(migration.solvedPortalQuestions[0]=="Assets/Scenes/Shadowroot Depths.unity/portal-question/gate","Scene migration lost solved portal");
        string once=JsonUtility.ToJson(migration);MapSceneNames.NormalizeSave(migration);
        check(once==JsonUtility.ToJson(migration),"Repeated migration changed the save");
        return count+" discovery checks passed: visited-only travel, per-slot isolation, save reload, duplicates, and legacy compatibility. Player saves unchanged.";
    }
}
