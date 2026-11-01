using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit authoring operation; never creates NPCs at runtime or moves existing teachers.</summary>
public static class CodeMentorAuthoring
{
    const string Pack="Assets/Sprites/Characters/NPC/GandalfHardcore Character Asset Pack/";
    const string Folder="Assets/Prefab/NPC/Code Mentors";
    public static string RefreshPortraits()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Folder}))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var layers=prefab.GetComponent<MentorSpriteLayers>();if(layers==null)continue;
            var pixels=new Color[80*64];
            foreach(var layer in layers.layers)
            {
                var source=layer.frames[0].texture;
                var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var previous=RenderTexture.active;
                Graphics.Blit(source,rt);RenderTexture.active=rt;
                var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();
                RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
                var colors=copy.GetPixels(0,376,80,64);Object.DestroyImmediate(copy);
                for(int i=0;i<pixels.Length;i++)
                {
                    float a=colors[i].a+pixels[i].a*(1-colors[i].a);
                    if(a<=0)continue;
                    var c=(colors[i]*colors[i].a+pixels[i]*pixels[i].a*(1-colors[i].a))/a;c.a=a;pixels[i]=c;
                }
            }
            var full=new Texture2D(80,64,TextureFormat.RGBA32,false);full.SetPixels(pixels);full.Apply();
            var portrait=new Texture2D(32,55,TextureFormat.RGBA32,false);portrait.SetPixels(full.GetPixels(24,3,32,55));portrait.Apply();
            string path=Folder+"/"+prefab.name+" Portrait.png";File.WriteAllBytes(path,portrait.EncodeToPNG());Object.DestroyImmediate(full);Object.DestroyImmediate(portrait);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var actor=prefab.GetComponent<Npc_Talk>().actor;actor.portrait=AssetDatabase.LoadAssetAtPath<Sprite>(path);EditorUtility.SetDirty(actor);
        }
        AssetDatabase.SaveAssets();return "Updated three fully dressed shared-dialogue portraits.";
    }
    public static string CreateTeachers()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Scene 2.unity")throw new System.InvalidOperationException("Open Scene 2 before authoring the tutorial teachers.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        string[] names={"Alden - Scrollkeeper","Rowan - Gate Warden","Mira - Potion Scholar"};
        string[][] clothes={new[]{"Character skin colors/Male Skin1.png","Male Clothing/Blue Pants.png","Male Clothing/Blue Shirt v2.png","Male Clothing/Boots.png","Male Hair/Male Hair1.png"},
            new[]{"Character skin colors/Male Skin3.png","Male Clothing/Green Pants.png","Male Clothing/Green Shirt v2.png","Male Clothing/Shoes.png","Male Hair/Male Hair3.png"},
            new[]{"Character skin colors/Female Skin2.png","Female Clothing/Skirt.png","Female Clothing/Purple Corset v2.png","Female Clothing/Boots.png","Female Hair/Female Hair4.png"}};
        float[] xs={-2,35,73};int added=0;
        for(int teacher=0;teacher<3;teacher++)
        {
            if(Object.FindObjectsByType<CodeMentorNPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(n=>n.workshop==teacher))continue;
            string path=Folder+"/"+names[teacher]+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NPC/Shared Dialogue NPC.prefab"));
                go.name=names[teacher];go.GetComponent<Npc_Talk>().offersElfQuest=false;
                var visual=go.transform.Find("NPC Visual");visual.localScale=Vector3.one;
                var sorting=visual.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();sorting.sortingLayerName="Default";sorting.sortingOrder=2;
                foreach(var anim in go.GetComponentsInChildren<Animator>())anim.enabled=false;
                foreach(var renderer in go.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
                var layered=go.AddComponent<MentorSpriteLayers>();layered.layers=new MentorSpriteLayers.Layer[clothes[teacher].Length];
                for(int layer=0;layer<clothes[teacher].Length;layer++)
                {
                    string source=Pack+clothes[teacher][layer];
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);if(texture==null)throw new System.Exception("Missing mentor layer: "+source);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(source);
                    if(importer.filterMode!=FilterMode.Point){importer.filterMode=FilterMode.Point;importer.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);}
                    var sprites=new Sprite[5];
                    for(int f=0;f<5;f++)
                    {
                        string spritePath=Folder+"/"+teacher+"-"+layer+"-"+f+".asset";
                        sprites[f]=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if(sprites[f]==null){sprites[f]=Sprite.Create(texture,new Rect(f*80,376,80,64),new Vector2(.5f,7f/64f),16,0,SpriteMeshType.FullRect);sprites[f].name=names[teacher]+" layer "+layer+" idle "+f;AssetDatabase.CreateAsset(sprites[f],spritePath);}
                    }
                    var part=new GameObject(Path.GetFileNameWithoutExtension(source),typeof(SpriteRenderer));part.transform.SetParent(visual,false);
                    var sr=part.GetComponent<SpriteRenderer>();sr.sprite=sprites[0];sr.sortingLayerName="Default";sr.sortingOrder=20+layer;
                    layered.layers[layer]=new MentorSpriteLayers.Layer {renderer=sr,frames=sprites};
                }
                var actor=ScriptableObject.CreateInstance<ActorSO>();actor.actorName=names[teacher];actor.portrait=layered.layers[0].frames[0];
                AssetDatabase.CreateAsset(actor,Folder+"/"+names[teacher]+".asset");go.GetComponent<Npc_Talk>().actor=actor;
                go.AddComponent<CodeMentorNPC>().workshop=teacher;
                prefab=PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);
            }
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(instance,"Add visual code teacher");
            var hit=Physics2D.RaycastAll(new Vector2(xs[teacher],8),Vector2.down,20).FirstOrDefault(h=>!h.collider.isTrigger);
            instance.transform.position=new Vector3(xs[teacher],hit.collider!=null?hit.point.y:0,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);added++;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved "+added+" new teachers in Scene 2. Existing NPC placements preserved.";
    }
}
