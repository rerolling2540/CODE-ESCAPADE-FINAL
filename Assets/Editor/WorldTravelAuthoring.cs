using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldTravelAuthoring
{
    const string Art="Assets/Sprites/Pixel Art Asset - Portal Monolitic 3.0/portal/animation/sprites_single";
    const string Folder="Assets/Prefab/World Travel";
    static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static Vector3 Floor(Scene scene,Vector3 point)
    {
        Physics2D.SyncTransforms();
        var hit=Physics2D.RaycastAll(new Vector2(point.x,point.y+2),Vector2.down,16).Where(h=>h.collider!=null&&h.collider.gameObject.scene==scene&&!h.collider.isTrigger&&h.normal.y>.5f&&h.collider.GetComponentInParent<playerMovement>()==null&&h.collider.GetComponentInParent<Npc_Talk>()==null).OrderBy(h=>h.distance).FirstOrDefault();
        if(hit.collider==null)throw new Exception("No ground for monolith at "+scene.name+" "+point);return new Vector3(point.x,hit.point.y+.02f,0);
    }
    public static string PrepareAssets()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Assets/Resources/Travel");AssetDatabase.Refresh();
        var paths=Directory.GetFiles(Art,"*.png").OrderBy(p=>int.Parse(Path.GetFileNameWithoutExtension(p).Replace("Pixel Asset - Portal Monolithic Animation",""))).Select(p=>p.Replace('\\','/')).ToArray();
        AssetDatabase.StartAssetEditing();
        try{foreach(var path in paths){var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=48;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);i.SetTextureSettings(settings);i.SaveAndReimport();}}
        finally{AssetDatabase.StopAssetEditing();}
        var frames=paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();if(frames.Any(f=>f==null))throw new Exception("Monolith frames did not import as sprites.");
        const string catalogPath="Assets/Resources/Travel/WorldTravelCatalog.asset";var catalog=AssetDatabase.LoadAssetAtPath<WorldTravelCatalog>(catalogPath);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<WorldTravelCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
        catalog.destinations=new[]{
            new WorldTravelCatalog.Destination{sceneName="Scene 2",title="Scene 2 — Alden's camp",description="Output, variables, and the first battle"},
            new WorldTravelCatalog.Destination{sceneName="Scene3",title="Scene 3 — Rowan's hollow",description="Conditions, loops, and cave exploration"},
            new WorldTravelCatalog.Destination{sceneName="map 4",title="Map 4 — Mira's refuge",description="Arrays, lists, and functions"},
            new WorldTravelCatalog.Destination{sceneName="map 5",title="Map 5 — The Warden's grove",description="Explore the grove and prepare for the final trial"},
            new WorldTravelCatalog.Destination{sceneName="MainMenu",title="Lobby",description="Return to character selection and save slots",lobby=true}};EditorUtility.SetDirty(catalog);
        var go=new GameObject("Travel Monolith",typeof(BoxCollider2D),typeof(PortalInteraction),typeof(MonolithTravel));
        var visual=new GameObject("Animated Monolith",typeof(SpriteRenderer));visual.transform.SetParent(go.transform,false);var sr=visual.GetComponent<SpriteRenderer>();sr.sprite=frames[0];sr.sortingOrder=3;visual.transform.localScale=Vector3.one*(3.4f/frames[0].bounds.size.y);
        var travel=go.GetComponent<MonolithTravel>();travel.frames=frames;travel.portalVisual=sr;
        var box=go.GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(4.6f,4.2f);box.offset=new Vector2(0,1.7f);
        PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Travel Monolith.prefab");UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();return frames.Length+" monolith animation frames and five travel destinations configured.";
    }
    public static string ApplyScenes()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before authoring portals.");
        var active=SceneManager.GetActiveScene();
        if(active.isDirty)
        {
            const string temp="Assets/Editor/WorldTravelUnsavedBackup.unity";
            if(!EditorSceneManager.SaveScene(active,temp,true))throw new Exception("Could not back up unsaved scene.");
            File.Copy(temp,"C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc/Scene2-before-monolith.unity",true);AssetDatabase.DeleteAsset(temp);
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Travel Monolith.prefab");int portals=0,aids=0;
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if(opened&&scene.isDirty)throw new Exception("Unexpected unsaved changes in "+name);
            var old=All<WardenTravelGate>(scene);
            foreach(var gate in old)
            {
                var at=Floor(scene,gate.transform.position);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.name="Travel Monolith - "+name+" route";instance.transform.position=at;PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);UnityEngine.Object.DestroyImmediate(gate.gameObject);portals++;
            }
            var elf=All<Npc_Talk>(scene).First(n=>n.name=="Elf - Warden Guide");
            var primary=All<MonolithTravel>(scene).FirstOrDefault(m=>m.name=="Travel Monolith - Camp");
            if(primary==null)
            {
                if(name=="map 5")primary=All<MonolithTravel>(scene).FirstOrDefault();
                if(primary==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name="Travel Monolith - Camp";go.transform.position=Floor(scene,elf.transform.position+Vector3.right*(name=="map 4"?-13:name=="Scene3"?-12:12));PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);primary=go.GetComponent<MonolithTravel>();portals++;}
            }
            if(!All<PortalArrivalPoint>(scene).Any(a=>a.arrivalId==WorldTravelCatalog.ArrivalId))
            {
                var point=new GameObject("Monolith Safe Arrival",typeof(PortalArrivalPoint));point.transform.SetParent(primary.transform,false);point.transform.localPosition=new Vector3(3.8f,1.2f,0);var arrival=point.GetComponent<PortalArrivalPoint>();arrival.arrivalId=WorldTravelCatalog.ArrivalId;arrival.requireSafeLanding=true;
            }
            foreach(var aid in All<WardenVisualAid>(scene))
            {
                var at=elf.transform;aid.transform.SetParent(at,true);aid.transform.localPosition=new Vector3(name=="map 4"?-5:4,0,0);aid.teacher=elf;aid.contextualDisplay=true;
                
                var label=aid.caption;label.fontSize=3.6f;label.rectTransform.sizeDelta=new Vector2(7,1.8f);label.transform.localPosition=new Vector3(1.2f,2.4f,-.05f);label.alignment=TMPro.TextAlignmentOptions.Center;label.color=new Color(1,.94f,.73f);label.fontStyle=TMPro.FontStyles.Bold;label.textWrappingMode=TMPro.TextWrappingModes.Normal;label.enabled=false;
                foreach(var symbol in aid.symbols)if(symbol!=null)symbol.enabled=false;
                EditorUtility.SetDirty(aid);PrefabUtility.RecordPrefabInstancePropertyModifications(aid);PrefabUtility.RecordPrefabInstancePropertyModifications(aid.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(label);PrefabUtility.RecordPrefabInstancePropertyModifications(label.transform);foreach(var sr in aid.symbols)PrefabUtility.RecordPrefabInstancePropertyModifications(sr);aids++;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>File.Exists(s.path)).ToArray();
        GizmoUtility.SetIconEnabled(typeof(TMPro.TextMeshPro),false);GizmoUtility.SetIconEnabled(typeof(TMPro.TextMeshProUGUI),false);Selection.activeObject=null;
        AssetDatabase.SaveAssets();return "Saved "+portals+" animated monoliths and repaired "+aids+" learning displays. NPC placements preserved; missing Scene4 build entry removed; TextMeshPro editor icons hidden.";
    }
}


