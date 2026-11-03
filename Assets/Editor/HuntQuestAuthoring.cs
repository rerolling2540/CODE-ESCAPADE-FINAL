using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HuntQuestAuthoring
{
    public const string Folder="Assets/Prefab/NPC/Hollow Contracts";
    const string Pack="Assets/Sprites/Characters/NPC/EleonoreAndJoanna";
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before NPC authoring.");
        var active=SceneManager.GetActiveScene();var scenes=new List<Scene>();var opened=new List<Scene>();
        try
        {
            for(int i=0;i<2;i++)
            {
                string path="Assets/Scenes/"+HuntQuestProgress.Scene(i)+".unity";
                var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(scene);}
                if(scene.isDirty)throw new Exception("Unsaved edits in "+path+"; authoring did not overwrite them.");scenes.Add(scene);
                foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true)))
                    if(c.enemy==null)throw new Exception("Encounter without enemy data: "+c.name);
                var ids=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true)).Select(e=>e.name).ToHashSet();
                for(int stage=0;stage<3;stage++)foreach(string id in HuntQuestProgress.Get(i,stage).encounterIds)
                    if(!ids.Contains(id))throw new Exception("Missing contract target: "+id);
                var hunts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HuntQuestNPC>(true)).ToArray();
                if(hunts.Length>1||hunts.Any(h=>h.giver!=i))throw new Exception("Unexpected duplicate or wrong-map combat NPC: "+path);
            }
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var prefabs=PreparePrefabs();
            string backup="C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc";
            for(int i=0;i<2;i++)
            {
                var scene=scenes[i];if(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HuntQuestNPC>(true)).Any())continue;
                // Ground placement only applies to a new NPC. Re-running never moves an authored instance.
                var near=i==0?new Vector3(276,-16,0):new Vector3(-6,-2,0);
                Physics2D.SyncTransforms();
                var ground=Physics2D.RaycastAll(new Vector2(near.x,near.y+1),Vector2.down,12)
                    .Where(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.gameObject.scene==scene&&h.collider.CompareTag("Ground")&&h.normal.y>.5f).OrderBy(h=>h.distance).ToArray();
                if(ground.Length==0)throw new Exception("No walkable ground near "+near+" in "+scene.name);
                EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+"-before-hunt-NPC.unity",true);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);go.name=HuntQuestProgress.Name(i)+" - Hollow Contracts";
                Undo.RegisterCreatedObjectUndo(go,"Place combat quest NPC");go.transform.position=new Vector3(near.x,ground[0].point.y+.03f,0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new Exception("Unable to save "+scene.path);
            }
            AssetDatabase.SaveAssets();return "Saved Eleonore in Scene 2 and Joanna in Scene3 as shared-dialogue prefab instances. Re-running preserves their chosen positions.";
        }
        finally{if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);foreach(var scene in opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static GameObject[] PreparePrefabs()
    {
        var result=new GameObject[2];var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            for(int giver=0;giver<2;giver++)
            {
                string name=HuntQuestProgress.Name(giver),path=Folder+"/"+name+" - Hollow Contracts.prefab";
                result[giver]=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(result[giver]!=null)continue;
                string first=Pack+"/Sprites/"+name+"/Idle/Idle1.png";
                var pixels=new Texture2D(2,2);ImageConversion.LoadImage(pixels,File.ReadAllBytes(first));
                int minY=pixels.height,maxY=0;var colors=pixels.GetPixels32();
                for(int y=0;y<pixels.height;y++)for(int x=0;x<pixels.width;x++)if(colors[y*pixels.width+x].a>32){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                float pivotY=(float)minY/pixels.height,scale=2.7f*16/Math.Max(1,maxY-minY+1);UnityEngine.Object.DestroyImmediate(pixels);
                int count=giver==0?9:6;var frames=new Sprite[count];
                for(int f=0;f<count;f++)frames[f]=Import(Pack+"/Sprites/"+name+"/Idle/Idle"+(f+1)+".png",pivotY);
                string portrait=Pack+"/Portraits/"+name+"/"+(giver==0?"Eleonore1.png":"JoannaPortrait1(NOBG)1.png");
                var actor=ScriptableObject.CreateInstance<ActorSO>();actor.actorName=name;actor.portrait=Import(portrait,0);AssetDatabase.CreateAsset(actor,Folder+"/"+name+".asset");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NPC/Shared Dialogue NPC.prefab"),temp);
                try
                {
                    go.name=name+" - Hollow Contracts";var talk=go.GetComponent<Npc_Talk>();talk.offersElfQuest=false;talk.actor=actor;talk.dialogueSO=null;
                    go.AddComponent<HuntQuestNPC>().giver=giver;
                    var visual=go.transform.Find("NPC Visual");visual.localScale=Vector3.one*scale;visual.localPosition=Vector3.zero;
                    var renderer=visual.GetComponent<SpriteRenderer>();renderer.sprite=frames[0];renderer.sortingLayerName="Default";renderer.sortingOrder=2;renderer.flipX=false;
                    var animation=visual.GetComponent<NPCSpriteAnimation>();if(animation==null)animation=visual.gameObject.AddComponent<NPCSpriteAnimation>();animation.frames=frames;animation.framesPerSecond=7;
                    foreach(var animator in go.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                    var prompt=go.GetComponent<NPCInteractionPrompt>();if(prompt==null)prompt=go.AddComponent<NPCInteractionPrompt>();prompt.height=3.1f;
                    result[giver]=PrefabUtility.SaveAsPrefabAsset(go,path);
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        finally{EditorSceneManager.CloseScene(temp,true);}
        return result;
    }
    static Sprite Import(string path,float pivot)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Missing NPC art: "+path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,pivot);importer.SetTextureSettings(settings);
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
