using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Isolated Edit Mode checks; never accepts quests or writes player progress.</summary>
public static class QuestLearningRouteChecks
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Fields).SetValue(obj,value);
    static T Get<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Fields).GetValue(obj);
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Run these checks in Edit Mode.");
        int checks=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        var save=new SaveSlots.Record{wardenAccepted=true};
        check(QuestLearningRoute.Next(null).Contains("accept"),"Missing invitation for new player");
        check(QuestLearningRoute.Next(save).Contains("Showing messages"),"First step is not output");
        save.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,0,0));
        check(QuestLearningRoute.Next(save).Contains("Storing values"),"Completed output not skipped");
        save.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,1,0));
        check(QuestLearningRoute.Next(save).Contains("Try the chapter challenge"),"Missing application step");
        check(QuestJournalText.Main(save).Contains("[DONE]"),"Journal does not mark completed lessons");
        save.wardenProofs.Add(WardenQuest.Proof("supplies"));
        check(WardenQuest.Ready(save)&&QuestLearningRoute.Next(save).Contains("claim"),"Chapter reward not offered");
        for(int c=0;c<6;c++)
        {
            var empty=new SaveSlots.Record{wardenAccepted=true,wardenChapter=c};
            check(!QuestLearningRoute.Chapter(empty).All(s=>s.done),"Empty chapter already complete "+c);
            var full=new SaveSlots.Record{wardenAccepted=true,wardenChapter=c};
            for(int t=0;t<6;t++)full.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,t,0));
            foreach(string proof in new[]{"supplies","gate","fires","shelf","heal","tutorial-report","final-report"})full.wardenProofs.Add(WardenQuest.Proof(proof));
            full.defeatedEncounters.AddRange(new[]{BattleTutorialGuide.EncounterId,WardenQuest.FinalEncounter});
            check(QuestLearningRoute.Chapter(full).All(s=>s.done)&&WardenQuest.Ready(full),"Checklist disagrees with quest readiness "+c);
            // Remove each requirement individually to catch skipped battle reports or lesson receipts.
            foreach(var id in full.workshopExercisesCompleted.ToArray())
            {
                full.workshopExercisesCompleted.Remove(id);
                check(QuestLearningRoute.Chapter(full).All(s=>s.done)==WardenQuest.Ready(full),"Lesson parity failed "+c+" / "+id);
                full.workshopExercisesCompleted.Add(id);
            }
            foreach(var id in full.wardenProofs.ToArray())
            {
                full.wardenProofs.Remove(id);
                check(QuestLearningRoute.Chapter(full).All(s=>s.done)==WardenQuest.Ready(full),"Proof parity failed "+c+" / "+id);
                full.wardenProofs.Add(id);
            }
        }
        foreach(string language in new[]{"java","csharp","python"})for(int topic=0;topic<2;topic++)
        {
            var original=VisualCodeLessons.Get(language,topic);
            for(int value=0;value<=6;value++)
            {
                var lesson=BeginnerWorkshopExperiment.Create(language,topic,value%3,value);
                check(lesson.challenge==original.challenge&&lesson.answers.SequenceEqual(original.answers)&&lesson.correct==original.correct,"Experiment altered assessment");
                string expected=topic==0?BeginnerWorkshopExperiment.Messages[value%3]+"\nBegin your quest.":(5+value).ToString();
                check(lesson.steps.Last().output==expected,"Experiment trace gives wrong result");
                check(lesson.steps.All(s=>s.line<lesson.code.Split('\n').Length),"Invalid highlighted code line");
            }
        }
        string persisted=JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active));
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var go=new GameObject("Isolated workshop test",typeof(CodeMentorNPC),typeof(CodeWorkshopUI));var mentor=go.GetComponent<CodeMentorNPC>();var ui=go.GetComponent<CodeWorkshopUI>();
            Set(ui,"mentor",mentor);Set(ui,"language","java");Set(ui,"font",Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"));
            typeof(CodeWorkshopUI).GetMethod("Build",Fields).Invoke(ui,null);Set(ui,"opened",true);Set(ui,"page",2);
            foreach(string language in new[]{"java","csharp","python"})for(int topic=0;topic<2;topic++)
            {
                Set(ui,"language",language);Set(ui,"topic",topic);Set(ui,"exerciseTier",0);Set(ui,"step",0);Set(ui,"experimentMessage",0);Set(ui,"experimentCoins",0);
                Set(ui,"lesson",BeginnerWorkshopExperiment.Create(language,topic,0,0));
                typeof(CodeWorkshopUI).GetMethod("Refresh",Fields).Invoke(ui,null);
                Get<UnityEngine.UI.Button>(ui,"changeExample").onClick.Invoke();
                string output=Get<TMPro.TMP_Text>(ui,"console").text;
                check(output.Contains(topic==0?"Lantern ready!":"6"),"Actual experiment button did not update console");
                check(Get<int>(ui,"page")==2,"Experiment turned page automatically");
                check(!Get<bool>(ui,"answered")&&Get<string>(ui,"attempt")==null,"Experiment created a question attempt");
                if(language=="java")Capture(Get<GameObject>(ui,"root"),topic==0?"Alden-Output-Experiment.png":"Alden-Coins-Experiment.png",check);
                Get<UnityEngine.UI.Button>(ui,"resetExample").onClick.Invoke();
                check(Get<TMPro.TMP_Text>(ui,"console").text.Contains(topic==0?"Welcome, hero!":"5"),"Reset did not restore base example");
            }
            Set(ui,"exerciseTier",1);Set(ui,"lesson",WorkshopVariations.Get("python",1,1));Set(ui,"step",0);
            typeof(CodeWorkshopUI).GetMethod("Refresh",Fields).Invoke(ui,null);
            check(!Get<UnityEngine.UI.Button>(ui,"changeExample").gameObject.activeSelf,"Foundation toy shown on Applied exercise");
            check(Get<GameObject>(ui,"root").GetComponent<CanvasGroup>()!=null,"Workshop bypasses parent canvas group");
            Set(ui,"opened",false);
        }
        finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        check(persisted==JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active)),"Checks modified player progress");
        return checks+" checks passed: next objectives, all chapter prerequisite parity, three-language experiments, actual UI buttons/reset, manual page navigation, separate assessment, text layout and save preservation. Rendered two Alden previews.";
    }
    static void Capture(GameObject root,string file,Action<bool,string> check)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        var go=new GameObject("Preview camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.16f);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();
        try
        {
            foreach(var label in root.GetComponentsInChildren<TMPro.TMP_Text>())
            {label.ForceMeshUpdate();check(!label.isTextOverflowing,"Overflowing workshop label: "+label.name);}
            camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc/"+file,tex.EncodeToPNG());}
            finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);}
        }
        finally{canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
