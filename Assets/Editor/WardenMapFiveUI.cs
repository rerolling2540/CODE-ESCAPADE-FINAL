using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WardenMapFiveUI
{
    public static string Apply()
    {
        var active=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene("Assets/Scenes/map 4.unity",OpenSceneMode.Additive);var target=EditorSceneManager.OpenScene("Assets/Scenes/map 5.unity",OpenSceneMode.Additive);
        if(target.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ExplorationHUD>(true)).Any()){EditorSceneManager.CloseScene(source,true);EditorSceneManager.CloseScene(target,true);return "Map 5 already has exploration UI.";}
        string[] names={"AudioManager","Exploration HUD and Inventory","Setting UI","EventSystem","Canvas","Exploration Health HUD","Quest Journal HUD","LobbyConfirmationManager","palitan ng sine"};
        var roots=source.GetRootGameObjects();var selected=roots.Where(r=>names.Contains(r.name)).ToArray();if(selected.Length!=names.Length)throw new Exception("Missing reusable Map 4 UI root.");
        var indices=selected.ToDictionary(r=>r,r=>r.transform.GetSiblingIndex());var group=new GameObject("Shared Exploration UI");SceneManager.MoveGameObjectToScene(group,source);GameObject clone=null;
        try
        {
            foreach(var root in selected)root.transform.SetParent(group.transform,true);
            clone=UnityEngine.Object.Instantiate(group);SceneManager.MoveGameObjectToScene(clone,target);clone.name="Shared Exploration UI";
        }
        finally
        {
            foreach(var root in selected)root.transform.SetParent(null,true);foreach(var root in selected.OrderBy(r=>indices[r]))root.transform.SetSiblingIndex(indices[root]);UnityEngine.Object.DestroyImmediate(group);
        }
        // Keep cloned menu callbacks, but discard the source map's physical scene-exit trigger.
        var navigator=clone.transform.Find("palitan ng sine");if(navigator!=null){foreach(var c in navigator.GetComponents<Collider2D>())UnityEngine.Object.DestroyImmediate(c);foreach(var r in navigator.GetComponents<Renderer>())UnityEngine.Object.DestroyImmediate(r);navigator.name="Menu Scene Navigation";}
        string path="Assets/Prefab/NPC/Warden Journey/Shared Exploration UI.prefab";PrefabUtility.SaveAsPrefabAssetAndConnect(clone,path,InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(source);EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(source,true);EditorSceneManager.CloseScene(target,true);return "Saved existing inventory, almanac, settings, health HUD, quest journal, event system, and menu navigation in Map 5.";
    }
}
