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
            check(!WardenQuest.ApplyReward(s,chapter+1),"Wrong chapter paid");
            check(WardenQuest.ApplyReward(s,chapter),"Valid chapter not paid");
            check(!WardenQuest.ApplyReward(s,chapter),"Duplicate reward paid");
        }
        check(s.wardenChapter==6&&!WardenQuest.Ready(s),"Campaign did not finish");
        check(s.level==7&&s.experience==50,"XP curve/reward total mismatch");
        var roundTrip=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(s));check(roundTrip.wardenChapter==6&&roundTrip.wardenProofs.Count==7,"Save round trip failed");
        var old=JsonUtility.FromJson<SaveSlots.Record>("{\"elfQuestStage\":4,\"level\":6}");check(old.elfQuestStage==4&&WardenQuest.Chapter(old)==0&&!WardenQuest.Ready(old),"Old save lost legacy state");
        var wrongLanguage=new SaveSlots.Record{wardenAccepted=true};wrongLanguage.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language=="java"?"python":"java",0,0));check(!WardenQuest.Lesson(wrongLanguage,0),"Other-language lesson leaked");
        var fields=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;var type=typeof(CharacterInventory).GetNestedType("SaveData",BindingFlags.NonPublic);var inv=Activator.CreateInstance(type,true);var grant=typeof(CharacterInventory).GetMethod("ApplyWardenGrant",BindingFlags.NonPublic|BindingFlags.Static);
        check((bool)grant.Invoke(null,new object[]{inv,3,100,3,0}),"Inventory grant failed");check(!(bool)grant.Invoke(null,new object[]{inv,3,100,3,0}),"Inventory reward repeated");check((int)type.GetField("coins",fields).GetValue(inv)==100&&(int)type.GetField("lowLogicScraps",fields).GetValue(inv)==3,"Inventory balances wrong");
        type.GetField("coins",fields).SetValue(inv,int.MaxValue);grant.Invoke(null,new object[]{inv,4,150,0,1});check((int)type.GetField("coins",fields).GetValue(inv)==int.MaxValue,"Gold overflow");
        foreach(string language in new[]{"java","csharp","python"})foreach(int topic in Enumerable.Range(0,6))foreach(int tier in Enumerable.Range(1,2)){var lesson=WorkshopVariations.Get(language,topic,tier);check(lesson.correct>=0&&lesson.correct<lesson.answers.Length&&!string.IsNullOrWhiteSpace(lesson.answers[lesson.correct]),"Invalid final question");}
        var active=SceneManager.GetActiveScene();
        foreach(string name in new[]{"Scene 2","Scene3","map 4","map 5"})
        {
            string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var talks=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Npc_Talk>(true)).ToArray();check(talks.Any(t=>t.offersElfQuest),"Missing elf: "+name);check(talks.Where(t=>t.offersElfQuest).All(t=>PrefabUtility.IsPartOfPrefabInstance(t)),"Elf is not selectable prefab instance");
            var teachers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CodeMentorNPC>(true)).ToArray();int expected=name=="Scene 2"?0:name=="Scene3"?1:2;check(name=="map 5"?teachers.Length==0:teachers.Length==1&&teachers[0].workshop==expected,"Teacher placement mismatch "+name);
            check(CharacterAlmanac.IsExplorationScene(name),"Missing exploration support");
            check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<CharacterInventory>(true).Length)==1,"Missing/duplicate inventory "+name);
            check(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<QuestJournalHud>(true).Length)==1,"Missing/duplicate journal "+name);
            if(name=="map 5")foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))for(int i=0;i<b.onClick.GetPersistentEventCount();i++)check(b.onClick.GetPersistentTarget(i)!=null,"Missing Map 5 menu target: "+b.name);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);check(EditorBuildSettings.scenes.Any(b=>b.path=="Assets/Scenes/map 5.unity"&&b.enabled),"Map 5 absent from build");
        return checks+" checks passed: chapter prerequisites, rewards, duplicates, XP, save compatibility, language isolation, inventory overflow, question validity, and authored scene placement. No player saves modified.";
    }
}
