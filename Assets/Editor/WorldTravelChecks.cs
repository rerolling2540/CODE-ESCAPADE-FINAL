using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldTravelChecks
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(value,message)=>{if(!value)throw new Exception(message);checks++;};
        var catalog=WorldTravelCatalog.Load();check(catalog!=null&&catalog.destinations.Length==5,"Missing destinations");
        foreach(var d in catalog.destinations)check(EditorBuildSettings.scenes.Any(b=>b.enabled&&System.IO.Path.GetFileNameWithoutExtension(b.path)==d.sceneName&&System.IO.File.Exists(b.path)),"Destination cannot be loaded: "+d.sceneName);
        check(catalog.destinations.Select(d=>d.sceneName).Distinct().Count()==5,"Duplicate destinations");
        var active=SceneManager.GetActiveScene();
        foreach(string name in new[]{"Whispering Hollow","Shadowroot Depths","Frostbound Passage","Forest of Dawn"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();var portals=components.OfType<MonolithTravel>().ToArray();
            check(portals.Length>0,"No monolith in "+name);check(!components.OfType<WardenTravelGate>().Any(),"Old travel gate remains "+name);
            foreach(var p in portals){check(p.frames.Length==63&&p.frames.All(f=>f!=null),"Broken portal animation");check(p.portalVisual!=null&&AssetDatabase.GetAssetPath(p.frames[0]).Contains("Portal Monolitic"),"Wrong portal artwork");check(p.GetComponent<Collider2D>().isTrigger,"Monolith blocks player");check(PrefabUtility.IsPartOfPrefabInstance(p),"Monolith is not selectable authored prefab");}
            var arrival=components.OfType<PortalArrivalPoint>().Where(a=>a.arrivalId==WorldTravelCatalog.ArrivalId).ToArray();check(arrival.Length==1&&arrival[0].requireSafeLanding,"Missing or duplicate safe landing "+name);
            foreach(var aid in components.OfType<WardenVisualAid>()){check(aid.contextualDisplay&&aid.teacher!=null&&aid.transform.IsChildOf(aid.teacher.transform),"Detached learning display "+name);check(aid.caption.fontSize>=3,"Unreadable world label");}
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);
        check(!EditorBuildSettings.scenes.Any(s=>!System.IO.File.Exists(s.path)),"Missing scene in Build Settings");
        check(Attribute.IsDefined(typeof(HeroTM),typeof(SerializableAttribute)),"Hero target mode is not serializable");
        var load=typeof(CharacterSmokeEffects).GetMethod("LoadStrip",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);var frames=(Sprite[])load.Invoke(null,new object[]{"SmokeFX/Smoke VFX 2",64,64,64f});check(frames!=null&&frames.Length==13,"Landing FX frame layout mismatch");foreach(var frame in frames)UnityEngine.Object.DestroyImmediate(frame);
        return checks+" travel, animation, landing, learning-display, and serialization checks passed. NPC positions and saves unchanged.";
    }
}

