using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ElfGuidePreview
{
    const string Output="C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc/Elf-Guide-Fixed.png";
    public static string Run()
    {
        var original=UnityEngine.Object.FindObjectsByType<Npc_Talk>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(n=>n.name=="NPC"&&n.offersElfQuest);
        var journal=UnityEngine.Object.FindFirstObjectByType<QuestJournalHud>(FindObjectsInactive.Include);
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var a=UnityEngine.Object.Instantiate(original.gameObject);a.transform.position=new Vector3(-2,-1,0);
            var b=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NPC/Warden Journey/Elf - Warden Guide.prefab"),scene);b.transform.position=new Vector3(2,-1,0);
            foreach(var go in new[]{a,b})foreach(var renderer in go.GetComponentsInChildren<SpriteRenderer>(true)){renderer.gameObject.layer=31;if(renderer.name=="InteractIcon")renderer.enabled=false;}
            var cameraGo=new GameObject("Elf preview camera",typeof(Camera));var cam=cameraGo.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.7f;cam.transform.position=new Vector3(0,.3f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.15f,.19f);cam.cullingMask=1<<31;
            var rt=new RenderTexture(960,540,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();System.IO.File.WriteAllBytes(Output,tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(cameraGo);
            var clone=UnityEngine.Object.Instantiate(journal.gameObject);var hud=clone.GetComponent<QuestJournalHud>();hud.ConfigureLayout();var so=new SerializedObject(hud);var panel=(RectTransform)so.FindProperty("panel").objectReferenceValue;panel.SetParent(clone.transform,false);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);panel.anchoredPosition=new Vector2(280,-150);panel.sizeDelta=new Vector2(300,200);panel.gameObject.SetActive(true);var group=panel.GetComponent<CanvasGroup>();group.alpha=1;clone.GetComponent<CanvasGroup>().alpha=1;Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];panel.GetWorldCorners(corners);var p=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[2])*.5f);var sample=new Rect(p.x-8,Screen.height-p.y-8,16,16);
            if(!QuestJournalHud.OccludesWorldRect(sample))throw new Exception("Visible journal failed to hide a marker");
            group.alpha=0;if(QuestJournalHud.OccludesWorldRect(sample))throw new Exception("Hidden journal still blocks marker");group.alpha=1;clone.GetComponent<CanvasGroup>().alpha=0;if(QuestJournalHud.OccludesWorldRect(sample))throw new Exception("Hidden parent canvas still blocks marker");
            UnityEngine.Object.DestroyImmediate(clone);
        }
        finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        return "Rendered original elf and repaired guide. Three marker occlusion checks passed (visible panel, hidden panel, hidden parent). No saves or NPC placements changed.";
    }
}
