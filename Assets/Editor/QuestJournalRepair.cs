using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class QuestJournalRepair
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before saving journal repairs.");var active=SceneManager.GetActiveScene();int repaired=0;
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if(scene.isDirty)throw new Exception("Unsaved scene changes: "+name);
            foreach(var h in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuestJournalHud>(true)))
            {h.ConfigureLayout();EditorUtility.SetDirty(h);PrefabUtility.RecordPrefabInstancePropertyModifications(h);foreach(var c in h.GetComponentsInChildren<Component>(true))if(c!=null){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}repaired++;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);return "Saved repaired journal layout and references in "+repaired+" scenes.";
    }
    public static string Checks()
    {
        int checks=0;Action<bool,string> check=(v,msg)=>{if(!v)throw new Exception(msg);checks++;};
        var save=new SaveSlots.Record();check(QuestReturnPanel.Ready(save).Count==0,"New save offers teleport");
        save.wardenAccepted=true;save.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,0,0));save.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,1,0));save.wardenProofs.Add(WardenQuest.Proof("supplies"));save.learningQuestsAccepted.Add(LearningQuests.Key(0));
        var ready=QuestReturnPanel.Ready(save);check(ready.Count==2,"Main and teacher rewards not both offered");check(ready.Any(d=>d.giver==-1)&&ready.Any(d=>d.giver==0),"Wrong quest destinations");
        save.learningQuestsClaimed.Add(LearningQuests.Key(0));check(QuestReturnPanel.Ready(save).Count==1,"Claimed teacher still offered");
        check(QuestJournalText.All(save).Contains("REWARD READY"),"Missing ready status");check(QuestJournalText.All(save).Contains("<b>Reward</b>"),"Missing reward separation");
        var round=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(save));check(QuestReturnPanel.Ready(round).Count==1,"Readiness lost after reload");
        save.wardenChapter=6;check(QuestReturnPanel.Ready(save).Count==0,"Finished campaign still offers reward travel");
        var active=SceneManager.GetActiveScene();
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            var path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var h=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuestJournalHud>(true)).Single();var so=new SerializedObject(h);
            foreach(string field in new[]{"panel","slideRoot","arrowButton","objectiveLabel"})check(so.FindProperty(field).objectReferenceValue!=null,"Missing journal reference "+name+"/"+field);
            var scroll=h.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);check(scroll!=null&&scroll.content!=null&&scroll.viewport!=null,"Broken scroll references "+name);
            check(scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>()!=null,"Missing content sizing");
            check(h.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name=="Return to quest NPC"),"Missing manual return action");
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);return checks+" quest journal and reward-return checks passed without modifying player saves.";
    }
}
