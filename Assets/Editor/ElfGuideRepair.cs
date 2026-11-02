using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ElfGuideRepair
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before repairing the saved prefab.");
        var source=UnityEngine.Object.FindObjectsByType<Npc_Talk>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(n=>n.name=="NPC"&&n.offersElfQuest);
        const string path="Assets/Prefab/NPC/Warden Journey/Elf - Warden Guide.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Configure(prefab,source);
            PrefabUtility.SaveAsPrefabAsset(prefab,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();return "Guide prefab now uses the original elf sprite, idle controller, portrait, and main-quest identity. All guide instances inherit the repair; NPC placements are unchanged.";
    }
    public static void Configure(GameObject prefab,Npc_Talk source)
    {
        var elf=source.transform.Find("Elf NPC");if(elf==null)throw new Exception("Original elf visual not found.");
        var renderer=elf.GetComponent<SpriteRenderer>();var animation=elf.GetComponent<Animator>();
        var visual=prefab.transform.Find("NPC Visual");var sr=visual.GetComponent<SpriteRenderer>();sr.sprite=renderer.sprite;sr.sharedMaterial=renderer.sharedMaterial;sr.color=renderer.color;sr.flipX=renderer.flipX^(source.transform.lossyScale.x<0);sr.flipY=renderer.flipY;sr.sortingLayerID=renderer.sortingLayerID;sr.sortingOrder=renderer.sortingOrder;
        visual.localPosition=elf.localPosition;visual.localScale=new Vector3(Mathf.Abs(source.transform.localScale.x),Mathf.Abs(source.transform.localScale.y),1);
        var animator=visual.GetComponent<Animator>();if(animator==null)animator=visual.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=animation.runtimeAnimatorController;animator.enabled=true;
        var talk=prefab.GetComponent<Npc_Talk>();talk.actor=source.actor!=null?source.actor:source.dialogueSO.lines[0].speaker;talk.dialogueSO=source.dialogueSO;
    }
    public static string Check()
    {
        int checks=0;var active=SceneManager.GetActiveScene();var source=UnityEngine.Object.FindObjectsByType<Npc_Talk>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(n=>n.name=="NPC"&&n.offersElfQuest);var expected=source.transform.Find("Elf NPC").GetComponent<Animator>().runtimeAnimatorController;
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            string path="Assets/Scenes/"+name+".unity";var s=SceneManager.GetSceneByPath(path);bool opened=!s.IsValid()||!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var n in s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Npc_Talk>(true)).Where(n=>n.name=="Elf - Warden Guide"))
            {
                var sprite=n.GetComponentInChildren<SpriteRenderer>(true);if(sprite==null||!AssetDatabase.GetAssetPath(sprite.sprite).Contains("/Idle2/"))throw new Exception("Wrong elf art in "+name);checks++;
                if(n.GetComponentInChildren<Animator>(true).runtimeAnimatorController!=expected)throw new Exception("Wrong elf animation in "+name);checks++;
                if(n.actor==null||n.actor.actorName.ToLowerInvariant().Contains("blacksmith"))throw new Exception("Wrong elf portrait/name in "+name);checks++;
                if(!PrefabUtility.IsPartOfPrefabInstance(n))throw new Exception("Guide is not an authored prefab");checks++;
            }
            foreach(var n in s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CodeMentorNPC>(true))){if(n.GetComponent<MentorSpriteLayers>()==null)throw new Exception("Missing teacher layers");checks++;}
            if(opened)EditorSceneManager.CloseScene(s,true);
        }
        SceneManager.SetActiveScene(active);return checks+" elf and teacher visual checks passed across all four maps.";
    }
}
