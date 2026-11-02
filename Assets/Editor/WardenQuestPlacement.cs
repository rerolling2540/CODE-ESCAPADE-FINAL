using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WardenQuestPlacement
{
    const string Folder="Assets/Prefab/NPC/Warden Journey";
    static IEnumerable<T> All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true));
    static Vector3 Floor(Scene s,Vector3 near)
    {
        Physics2D.SyncTransforms();
        var hits=Physics2D.RaycastAll(new Vector2(near.x,near.y+1),Vector2.down,12).Where(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.gameObject.scene==s&&h.normal.y>.5f&&h.collider.GetComponentInParent<playerMovement>()==null&&h.collider.GetComponentInParent<Npc_Talk>()==null).OrderBy(h=>h.distance).ToArray();
        if(hits.Length==0)throw new System.Exception("No ground at "+s.name+" "+near);
        return new Vector3(near.x,hits[0].point.y+.03f,0);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before authoring.");
        var active=SceneManager.GetActiveScene();var scenes=new List<Scene>();var opened=new List<Scene>();
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"}){string path="Assets/Scenes/"+name+".unity";var s=SceneManager.GetSceneByPath(path);if(!s.IsValid()||!s.isLoaded){s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(s);}if(s.isDirty)throw new System.Exception("Scene has unsaved changes: "+name);scenes.Add(s);}
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var sourceElf=All<Npc_Talk>(scenes[0]).First(n=>n.offersElfQuest&&n.transform.Find("Elf NPC")!=null);
        string elfPath=Folder+"/Elf - Warden Guide.prefab";
        var elfPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(elfPath);
        if(elfPrefab==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NPC/Shared Dialogue NPC.prefab"));go.name="Elf - Warden Guide";var talk=go.GetComponent<Npc_Talk>();talk.offersElfQuest=true;talk.actor=sourceElf.actor;talk.dialogueSO=sourceElf.dialogueSO;ElfGuideRepair.Configure(go,sourceElf);elfPrefab=PrefabUtility.SaveAsPrefabAsset(go,elfPath);Object.DestroyImmediate(go);}
        Vector3[] teacherNear={Vector3.zero,new Vector3(-11,-49,0),new Vector3(-26,-174,0)};
        for(int teacher=1;teacher<=2;teacher++)
        {
            var destination=scenes[teacher];var existing=All<CodeMentorNPC>(destination).FirstOrDefault(t=>t.workshop==teacher);
            if(existing!=null)continue;
            var moving=All<CodeMentorNPC>(scenes[0]).FirstOrDefault(t=>t.workshop==teacher);
            if(moving==null)throw new System.Exception("Missing teacher "+teacher);
            var position=Floor(destination,teacherNear[teacher]);moving.transform.SetParent(null);SceneManager.MoveGameObjectToScene(moving.gameObject,destination);moving.transform.position=position;PrefabUtility.RecordPrefabInstancePropertyModifications(moving.transform);
        }
        Vector3[] elfNear={new Vector3(5,-1,0),new Vector3(-17,-49,0),new Vector3(-32,-174,0),new Vector3(133,4,0)};
        for(int i=0;i<4;i++)
        {
            var s=scenes[i];var elf=All<Npc_Talk>(s).FirstOrDefault(n=>n.name=="Elf - Warden Guide");
            if(elf==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(elfPrefab,s);go.name="Elf - Warden Guide";go.transform.position=Floor(s,elfNear[i]);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);elf=go.GetComponent<Npc_Talk>();}
            if(i==0)Aid(s,elf.transform,"Supplies demonstration","supplies",new[]{"Coins"},"coins: 3 + 4 = ?","coins: 7");
            if(i==1){Aid(s,elf.transform,"Gate demonstration","gate",new[]{"Gate"},"keys: 2   /   required: at least 2","2 >= 2 is true: gate OPEN");Aid(s,elf.transform,"Watchfire demonstration","fires",new[]{"Step","Step","Step","Step"},"Counter visits: 0, 1, 2, 3","Four repeats: four watchfires lit",5);}
            if(i==2){Aid(s,elf.transform,"Potion shelf demonstration","shelf",new[]{"Heal","Heal","Heal"},"Index: 0 = 3    1 = 6    2 = 9","Index 1 selects the 6-point potion");Aid(s,elf.transform,"Healing demonstration","heal",new[]{"Heal"},"heal(input) returns input + 3","heal(5) returns 8",5);}
        }
        // Keep the user's original cave elf, Alden, and all other NPC placements untouched.
        var final=All<CaveEnemyEncounter>(scenes[3]).FirstOrDefault(e=>e.name==WardenQuest.FinalEncounter);
        if(final==null)
        {
            var template=All<CaveEnemyEncounter>(scenes[0]).First(e=>e.enemy!=null&&e.name==BattleTutorialGuide.EncounterId);
            var go=Object.Instantiate(template.gameObject);SceneManager.MoveGameObjectToScene(go,scenes[3]);go.name=WardenQuest.FinalEncounter;var e=go.GetComponent<CaveEnemyEncounter>();
            // Two familiar, low-damage training opponents keep the capstone approachable.
            var encounter=ScriptableObject.CreateInstance<EncounterData>();var so=new SerializedObject(encounter);var es=so.FindProperty("enemies");es.arraySize=2;es.GetArrayElementAtIndex(0).objectReferenceValue=template.enemy;es.GetArrayElementAtIndex(1).objectReferenceValue=template.enemy;
            so.ApplyModifiedPropertiesWithoutUndo();string data=Folder+"/Warden Final Trial.asset";AssetDatabase.CreateAsset(encounter,data);e.battleEncounter=encounter;e.patrolSpeed=0;e.patrolRadius=.5f;go.transform.position=Floor(scenes[3],new Vector3(147,4,0))+Vector3.up;
            string path=Folder+"/Warden Final Trial.prefab";PrefabUtility.SaveAsPrefabAssetAndConnect(go,path,InteractionMode.AutomatedAction);
        }
        Route(scenes[2],new Vector3(350,-139,0),"map 5","warden-map5",true);
        Route(scenes[3],new Vector3(125,4,0),"map 4","warden-map4",false);
        Arrival(scenes[2],"warden-map4",new Vector3(346,-139,0));Arrival(scenes[3],"warden-map5",new Vector3(129,4,0));
        var build=EditorBuildSettings.scenes.ToList();if(!build.Any(s=>s.path==scenes[3].path))build.Add(new EditorBuildSettingsScene(scenes[3].path,true));else build.First(s=>s.path==scenes[3].path).enabled=true;EditorBuildSettings.scenes=build.ToArray();
        foreach(var s in scenes){EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}AssetDatabase.SaveAssets();SceneManager.SetActiveScene(active);foreach(var s in opened)EditorSceneManager.CloseScene(s,true);
        return "Saved Alden in Scene 2, Rowan in Scene3, Mira in map 4, elf guides in all four maps, demonstrations, final encounter, and Map 4/5 return routes. Original cave elf preserved.";
    }
    static Sprite Icon(string name)
    {
        string path=Folder+"/"+name+" World Icon.asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
        var texture=Resources.Load<Texture2D>("UI/WorkshopArt/"+name);if(texture==null)throw new System.Exception("Missing icon "+name);
        sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0),texture.height);AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    static void Aid(Scene scene,Transform elf,string name,string proof,string[] icons,string before,string after,float offset=0)
    {
        if(All<WardenVisualAid>(scene).Any(a=>a.proof==proof))return;
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=Floor(scene,elf.position+Vector3.right*(4+offset));
        var aid=go.AddComponent<WardenVisualAid>();aid.proof=proof;aid.beforeText=before;aid.afterText=after;aid.symbols=new SpriteRenderer[icons.Length];
        for(int i=0;i<icons.Length;i++){var part=new GameObject("Code symbol "+i,typeof(SpriteRenderer));part.transform.SetParent(go.transform,false);part.transform.localPosition=new Vector3(i*1.15f,0,0);var sr=part.GetComponent<SpriteRenderer>();sr.sprite=Icon(icons[i]);sr.sortingOrder=4;aid.symbols[i]=sr;}
        var text=new GameObject("Learning state",typeof(TMPro.TextMeshPro));text.transform.SetParent(go.transform,false);text.transform.localPosition=new Vector3(1,2,0);var label=text.GetComponent<TMPro.TextMeshPro>();label.text=before;label.font=Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");label.fontSize=2;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(5,1.5f);label.sortingOrder=5;aid.caption=label;
        PrefabUtility.SaveAsPrefabAssetAndConnect(go,Folder+"/"+name+".prefab",InteractionMode.AutomatedAction);
    }
    static void Route(Scene s,Vector3 near,string target,string arrival,bool final)
    {
        if(All<WardenTravelGate>(s).Any(g=>g.destinationScene==target))return;
        var go=new GameObject("Warden route to "+target,typeof(BoxCollider2D),typeof(SpriteRenderer),typeof(WardenTravelGate));SceneManager.MoveGameObjectToScene(go,s);go.transform.position=Floor(s,near);var c=go.GetComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(3,3);c.offset=new Vector2(0,1.5f);var r=go.GetComponent<SpriteRenderer>();r.sprite=Icon("Gate");r.sortingOrder=5;go.transform.localScale=Vector3.one*2;var route=go.GetComponent<WardenTravelGate>();route.destinationScene=target;route.arrivalId=arrival;route.finalRoute=final;PrefabUtility.SaveAsPrefabAssetAndConnect(go,Folder+"/Route to "+target+".prefab",InteractionMode.AutomatedAction);
    }
    static void Arrival(Scene s,string id,Vector3 near)
    {if(All<PortalArrivalPoint>(s).Any(a=>a.arrivalId==id))return;var go=new GameObject("Warden arrival "+id,typeof(PortalArrivalPoint));SceneManager.MoveGameObjectToScene(go,s);go.transform.position=Floor(s,near)+Vector3.up;go.GetComponent<PortalArrivalPoint>().arrivalId=id;}
}

