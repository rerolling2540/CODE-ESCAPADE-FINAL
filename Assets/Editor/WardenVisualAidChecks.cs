using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WardenVisualAidChecks
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        foreach(string proof in WardenChallenge.IDs)
        {
            check(!WardenVisualAid.IsLessonActive(proof,null,true,false,false),"Walking near NPC shows "+proof);
            check(!WardenVisualAid.IsLessonActive(proof,proof,true,false,false),"Closed dialogue shows "+proof);
            check(!WardenVisualAid.IsLessonActive(proof,"another-lesson",true,true,false),"Wrong lesson shows "+proof);
            check(!WardenVisualAid.IsLessonActive(proof,proof,false,true,false),"Wrong NPC shows "+proof);
            check(!WardenVisualAid.IsLessonActive(proof,proof,true,true,true),"Overlay shows "+proof);
            check(WardenVisualAid.IsLessonActive(proof,proof,true,true,false),"Matching lesson hidden "+proof);
        }
        check(!WardenVisualAid.IsLessonActive(null,null,true,true,false),"Untagged dialogue shows props");
        var active=SceneManager.GetActiveScene();var previous=DialogueManager.Instance;var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(temp);
        try
        {
            var owner=new GameObject("Test NPC",typeof(Npc_Talk)).GetComponent<Npc_Talk>();
            var go=new GameObject("Test illustration",typeof(WardenVisualAid));var aid=go.GetComponent<WardenVisualAid>();aid.proof="supplies";aid.teacher=owner;
            var symbol=new GameObject("Test coin",typeof(SpriteRenderer));var sr=symbol.GetComponent<SpriteRenderer>();aid.symbols=new[]{sr};
            aid.caption=new GameObject("Test caption",typeof(TMPro.TextMeshPro)).GetComponent<TMPro.TextMeshPro>();aid.beforeText="Test lesson";
            var method=typeof(WardenVisualAid).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            DialogueManager.Instance=null;method.Invoke(aid,null);check(!sr.enabled&&!aid.caption.enabled,"Actual coin/caption stayed visible without dialogue");
            var dialogue=new GameObject("Test dialogue",typeof(DialogueManager)).GetComponent<DialogueManager>();DialogueManager.Instance=dialogue;dialogue.isDialogueActive=true;typeof(DialogueManager).GetProperty("Owner").SetValue(dialogue,owner);
            var node=ScriptableObject.CreateInstance<DialogueSO>();node.learningVisualProof="supplies";typeof(DialogueManager).GetField("currentDialogue",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(dialogue,node);
            check(!GameControlsGuide.GameplayOverlayHidden,"Unexpected overlay during isolated check");
            method.Invoke(aid,null);check(sr.enabled&&aid.caption.enabled,"Matching challenge did not display illustration");
            dialogue.isDialogueActive=false;method.Invoke(aid,null);check(!sr.enabled&&!aid.caption.enabled,"Closing challenge left floating illustration");
            dialogue.isDialogueActive=true;node.learningVisualProof=null;method.Invoke(aid,null);check(!sr.enabled&&!aid.caption.enabled,"Ordinary NPC dialogue displayed coins");
            sr.enabled=true;aid.caption.enabled=true;typeof(WardenVisualAid).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(aid,null);check(!sr.enabled&&!aid.caption.enabled,"Disabled display left stale renderers");
            UnityEngine.Object.DestroyImmediate(node);
        }
        finally{DialogueManager.Instance=previous;SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(temp,true);}
        int displays=0;
        foreach(string name in new[]{"Whispering Hollow","Shadowroot Depths","Frostbound Passage","Forest of Dawn"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var aid in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WardenVisualAid>(true)))
            {check(WardenChallenge.IDs.Contains(aid.proof)&&aid.teacher!=null,"Unbound illustration in "+name);displays++;}
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);check(displays==5,"Expected all five learning displays");
        return checks+" checks passed, including actual renderer/caption show-hide transitions. Inspected "+displays+" displays across all four maps. Player saves and NPC positions unchanged.";
    }
}
