using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WardenQuestAuthoring
{
    public static string Inspect()
    {
        var result=new List<string>();var active=SceneManager.GetActiveScene();
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.GetComponent<playerMovement>()!=null||t.GetComponent<Npc_Talk>()!=null||t.GetComponent<CaveEnemyEncounter>()!=null||t.name.ToLowerInvariant().Contains("portal")||t.name.ToLowerInvariant().Contains("teleport")||t.name.Contains("Spawn"))result.Add(name+": "+t.name+" "+t.position+(t.GetComponent<CaveEnemyEncounter>()!=null?" enemy="+t.GetComponent<CaveEnemyEncounter>().enemy:""));
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);return string.Join("\n",result);
    }
}
