using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WardenQuestChecks
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(value,message)=>{if(!value)throw new Exception(message);checks++;};
        var s=new SaveSlots.Record{wardenAccepted=true,level=5};
        check(!WardenQuest.Ready(s),"New quest should need lessons");
        foreach(int t in Enumerable.Range(0,6))s.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,t,0));
        foreach(string proof in new[]{"supplies","gate","fires","shelf","heal","tutorial-report","final-report"})s.wardenProofs.Add(WardenQuest.Proof(proof));
        s.defeatedEncounters.Add(BattleTutorialGuide.EncounterId);s.defeatedEncounters.Add(WardenQuest.FinalEncounter);
        for(int chapter=0;chapter<6;chapter++)
        {
            check(WardenQuest.Ready(s),"Chapter not ready: "+chapter);
            check(!WardenQuest.CanFightFinalBoss(s),"Final boss unlocked before claiming chapter "+chapter);
            check(!WardenQuest.ApplyReward(s,chapter+1),"Wrong chapter paid");
            check(WardenQuest.ApplyReward(s,chapter),"Valid chapter not paid");
            check(!WardenQuest.ApplyReward(s,chapter),"Duplicate reward paid");
        }
        check(s.wardenChapter==6&&!WardenQuest.Ready(s),"Campaign did not finish");
        check(WardenQuest.CanFightFinalBoss(s)&&!WardenQuest.CanFightFinalBoss(null),"Final boss unlock did not require completed quest");
        check(s.level==7&&s.experience==50,"XP curve/reward total mismatch");
        var roundTrip=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(s));check(roundTrip.wardenChapter==6&&roundTrip.wardenProofs.Count==7,"Save round trip failed");
        var old=JsonUtility.FromJson<SaveSlots.Record>("{\"elfQuestStage\":4,\"level\":6}");check(old.elfQuestStage==4&&WardenQuest.Chapter(old)==0&&!WardenQuest.Ready(old),"Old save lost legacy state");
        var wrongLanguage=new SaveSlots.Record{wardenAccepted=true};wrongLanguage.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language=="java"?"python":"java",0,0));check(!WardenQuest.Lesson(wrongLanguage,0),"Other-language lesson leaked");
        foreach(int chapter in new[]{1,5})
        {
            var interrupted=new SaveSlots.Record{wardenAccepted=true,wardenChapter=chapter,level=5};
            string encounter=chapter==1?BattleTutorialGuide.EncounterId:WardenQuest.FinalEncounter;
            string proof=chapter==1?"tutorial-report":"final-report";
            for(int t=0;t<6;t++)interrupted.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,t,0));
            check(WardenQuest.PendingBattleReview(interrupted)==null&&!WardenQuest.ApplyBattleReview(interrupted),"Review granted before victory");
            interrupted.defeatedEncounters.Add(encounter);
            interrupted=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(interrupted));
            check(!WardenQuest.Ready(interrupted)&&WardenQuest.PendingBattleReview(interrupted)==proof,"Interrupted victory cannot be reviewed after reload");
            check(QuestLearningRoute.Next(interrupted).Contains("elf"),"Journal does not explain review recovery");
            interrupted.wardenAccepted=false;check(!WardenQuest.ApplyBattleReview(interrupted),"Unaccepted quest granted review");interrupted.wardenAccepted=true;
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("Isolated battle review host");SceneManager.MoveGameObjectToScene(go,preview);
                var dialogue=WardenQuest.Dialogue(go.AddComponent<Npc_Talk>(),interrupted);
                check(dialogue.options[0].optionText==(chapter==1?"Review my training victory":"Review my final trial victory"),"Elf has no matching review option");
                var review=dialogue.options[0].nextDialogue;
                check(review!=null&&review.options.Any(o=>o.optionText=="Finish battle review"&&o.onSelected!=null),"Recovery review cannot be finished");
                // Npc_Talk owns these transient nodes; destroy them before closing the preview scene.
                foreach(var node in new[]{review,dialogue})UnityEngine.Object.DestroyImmediate(node);
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            check(WardenQuest.ApplyBattleReview(interrupted)&&WardenQuest.Ready(interrupted),"Review did not unblock chapter");
            check(interrupted.wardenChapter==chapter&&interrupted.experience==0,"Review paid a reward or skipped a chapter");
            check(!WardenQuest.ApplyBattleReview(interrupted)&&interrupted.wardenProofs.Count==1,"Review proof duplicated");
            check(WardenQuest.ApplyReward(interrupted,chapter)&&!WardenQuest.ApplyReward(interrupted,chapter),"Recovered reward cannot be claimed exactly once");
        }
        var fields=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;var type=typeof(CharacterInventory).GetNestedType("SaveData",BindingFlags.NonPublic);var inv=Activator.CreateInstance(type,true);var grant=typeof(CharacterInventory).GetMethod("ApplyWardenGrant",BindingFlags.NonPublic|BindingFlags.Static);
        check((bool)grant.Invoke(null,new object[]{inv,3,100,3,0}),"Inventory grant failed");check(!(bool)grant.Invoke(null,new object[]{inv,3,100,3,0}),"Inventory reward repeated");check((int)type.GetField("coins",fields).GetValue(inv)==100&&(int)type.GetField("lowLogicScraps",fields).GetValue(inv)==3,"Inventory balances wrong");
        type.GetField("coins",fields).SetValue(inv,int.MaxValue);grant.Invoke(null,new object[]{inv,4,150,0,1});check((int)type.GetField("coins",fields).GetValue(inv)==int.MaxValue,"Gold overflow");
        foreach(string language in new[]{"java","csharp","python"})foreach(int topic in Enumerable.Range(0,6))foreach(int tier in Enumerable.Range(1,2)){var lesson=WorkshopVariations.Get(language,topic,tier);check(lesson.correct>=0&&lesson.correct<lesson.answers.Length&&!string.IsNullOrWhiteSpace(lesson.answers[lesson.correct]),"Invalid final question");}
        var active=SceneManager.GetActiveScene();
        foreach(string name in new[]{"Whispering Hollow","Shadowroot Depths","Frostbound Passage","Forest of Dawn"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var talks=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Npc_Talk>(true)).ToArray();check(talks.Any(t=>t.offersElfQuest),"Missing elf: "+name);check(talks.Where(t=>t.offersElfQuest).All(t=>PrefabUtility.IsPartOfPrefabInstance(t)),"Elf is not selectable prefab instance");
            var teachers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CodeMentorNPC>(true)).ToArray();int expected=name=="Whispering Hollow"?0:name=="Shadowroot Depths"?1:2;check(name=="Forest of Dawn"?teachers.Length==0:teachers.Length==1&&teachers[0].workshop==expected,"Teacher placement mismatch "+name);
            check(CharacterAlmanac.IsExplorationScene(name),"Missing exploration support");
            check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<CharacterInventory>(true).Length)==1,"Missing/duplicate inventory "+name);
            check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<QuestJournalHud>(true).Length)==1,"Missing/duplicate journal "+name);
            if(name=="Whispering Hollow")
            {
                var encounters=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true))
                    .Where(e=>e.gameObject.activeInHierarchy).ToArray();
                var legacy=new SaveSlots.Record{elfQuestStage=3};
                legacy.defeatedEncounters.AddRange(encounters.Select(e=>e.name));
                check(ElfQuestProgress.Ready(legacy),"The earlier cave contract cannot be finished with active encounters");
                var tutorial=encounters.Single(e=>e.name==BattleTutorialGuide.EncounterId);
                check(tutorial.PermanentlyDefeated,"Training goblin respawns after a victory");
            }
            if(name=="Forest of Dawn")
            {
                var trials=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true))
                    .Where(e=>e.name==WardenQuest.FinalEncounter).ToArray();
                check(trials.Length==1,"Forest of Dawn must have one Warden Final Trial");
                var trial=trials[0];
                check(trial.isActiveAndEnabled&&PrefabUtility.IsPartOfPrefabInstance(trial),"Final trial is not an active prefab instance");
                check(trial.enemy!=null&&trial.battleEncounter!=null&&trial.battleEncounter.CreateEnemySquad().Count==2,"Final trial squad is incomplete");
                check(trial.PermanentlyDefeated,"Final trial respawns after a victory");
                foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))for(int i=0;i<b.onClick.GetPersistentEventCount();i++)check(b.onClick.GetPersistentTarget(i)!=null,"Missing Map 5 menu target: "+b.name);
            }
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);check(EditorBuildSettings.scenes.Any(b=>b.path=="Assets/Scenes/Forest of Dawn.unity"&&b.enabled),"Map 5 absent from build");
        return checks+" checks passed: chapter prerequisites, rewards, duplicates, XP, save compatibility, language isolation, inventory overflow, question validity, and authored scene placement. No player saves modified.";
    }
}
