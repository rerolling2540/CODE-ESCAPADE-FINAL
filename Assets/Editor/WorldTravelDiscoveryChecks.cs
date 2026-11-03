using System;
using UnityEngine;

public static class WorldTravelDiscoveryChecks
{
    public static string Run()
    {
        int count=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);count++;};
        var first=new SaveSlots.Record{scene="Scene 2"};
        check(WorldTravelDiscovery.HasVisited(first,"Scene 2"),"Starting map is not known");
        foreach(var map in new[]{"Scene3","map 4","map 5"})check(!WorldTravelDiscovery.HasVisited(first,map),"New save unlocked "+map);
        check(WorldTravelDiscovery.RecordVisit(first,"Scene3"),"First visit was not recorded");
        check(WorldTravelDiscovery.HasVisited(first,"Scene3"),"Visited map remains locked");
        check(!WorldTravelDiscovery.RecordVisit(first,"Scene3"),"Repeat visit created duplicate data");
        check(first.visitedScenes.Count==2,"Known initial map not retained");
        first.scene="Scene3";WorldTravelDiscovery.RecordVisit(first,"map 4");first.scene="map 4";
        check(WorldTravelDiscovery.HasVisited(first,"Scene 2")&&WorldTravelDiscovery.HasVisited(first,"Scene3"),"Returning maps lost discovery");
        check(!WorldTravelDiscovery.HasVisited(first,"map 5"),"Later map unlocked from earlier visit");
        var second=new SaveSlots.Record{scene="Scene 2"};check(!WorldTravelDiscovery.HasVisited(second,"map 4"),"Discovery leaked into another save");
        var loaded=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(first));check(WorldTravelDiscovery.HasVisited(loaded,"map 4")&&WorldTravelDiscovery.HasVisited(loaded,"Scene3"),"Discovery lost after reload");
        var old=new SaveSlots.Record{scene="Scene3",hasCheckpoint=true,checkpointScene="Scene 2",visitedScenes=null};
        check(WorldTravelDiscovery.HasVisited(old,"Scene3")&&WorldTravelDiscovery.HasVisited(old,"Scene 2"),"Old known maps not recognized");
        check(!WorldTravelDiscovery.HasVisited(old,"map 4"),"Old save granted unproven visit");
        WorldTravelDiscovery.RecordVisit(old,"map 4");old.scene="map 4";old.checkpointScene="map 4";
        check(WorldTravelDiscovery.HasVisited(old,"Scene3")&&WorldTravelDiscovery.HasVisited(old,"Scene 2"),"Migration lost old known maps");
        check(!WorldTravelDiscovery.RecordVisit(first,"Battle Scene"),"Battle scene counted as map discovery");
        check(!WorldTravelDiscovery.HasVisited(null,"Scene3"),"Empty slot unlocked a map");
        check(WorldTravelDiscovery.HasVisited(new SaveSlots.Record{scene="Scene4"},"map 4"),"Legacy Map 4 alias not recognized");
        check(MonolithTravelPanel.IsDiscovered(new WorldTravelCatalog.Destination{sceneName="MainMenu",lobby=true}),"Lobby locked");
        return count+" discovery checks passed: visited-only travel, per-slot isolation, save reload, duplicates, and legacy compatibility. Player saves unchanged.";
    }
}
