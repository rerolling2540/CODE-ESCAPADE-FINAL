using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class QuestReturnPreview
{
    public static string Render()
    {
        var active=SceneManager.GetActiveScene();var source=UnityEngine.Object.FindFirstObjectByType<QuestJournalHud>(FindObjectsInactive.Include);var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        int overflow=0;
        try
        {
            var s=new SaveSlots.Record{wardenAccepted=true,wardenChapter=4,elfQuestStage=1};
            for(int t=0;t<6;t++)s.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,t,0));
            for(int i=0;i<3;i++)s.learningQuestsAccepted.Add(LearningQuests.Key(i));
            foreach(var p in new[]{"shelf","heal"})s.wardenProofs.Add(WardenQuest.Proof(p));s.defeatedEncounters.AddRange(new[]{"Cave Skeleton 01","Cave Goblin 02","Cave Mushroom 03"});
            var host=new GameObject("Preview controller");var controller=host.AddComponent<QuestReturnPanel>();var root=controller.BuildPreview(QuestReturnPanel.Ready(s));
            Capture(root,"Quest-Return-Panel.png");
            foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.ForceMeshUpdate(true);if(text.isTextOverflowing)overflow++;}
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(host);
            var journal=UnityEngine.Object.Instantiate(source.gameObject);journal.name="Journal preview";var h=journal.GetComponent<QuestJournalHud>();h.ConfigureLayout();var so=new SerializedObject(h);var label=(TMPro.TMP_Text)so.FindProperty("objectiveLabel").objectReferenceValue;label.text=QuestJournalText.All(s);var panel=(RectTransform)so.FindProperty("panel").objectReferenceValue;panel.gameObject.SetActive(true);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,1);panel.anchoredPosition=new Vector2(-112,0);panel.GetComponent<CanvasGroup>().alpha=1;journal.GetComponent<CanvasGroup>().alpha=1;
            Capture(journal,"Quest-Journal-Readable.png");
            var scroll=h.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);if(scroll.content.rect.height<=scroll.viewport.rect.height)throw new Exception("Long journal content did not grow beyond viewport");
            UnityEngine.Object.DestroyImmediate(journal);
        }
        finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        if(overflow>0)throw new Exception(overflow+" overflowing return-panel labels");
        return "Rendered journal and five-ready-quest panel. No overflowing panel labels; long journal content scrolls. Player saves unchanged.";
    }
    static void Capture(GameObject root,string name)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        var cameraGo=new GameObject("Preview camera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.16f);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();var journal=root.GetComponent<QuestJournalHud>();if(journal!=null)typeof(QuestJournalHud).GetMethod("FitToCanvas",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(journal,null);Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc/"+name,tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(cameraGo);
    }
}

