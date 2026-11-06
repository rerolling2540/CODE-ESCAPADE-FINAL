using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyMapBalanceChecks
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Run isolated balance checks in Edit Mode.");
        int count=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);count++;};
        string before=JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active));
        var enemies=AssetDatabase.FindAssets("t:EnemyData").Select(g=>AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        string[] maps={"Whispering Hollow","Shadowroot Depths","Frostbound Passage","Forest of Dawn"};
        foreach(var enemy in enemies)
        {
            string original=JsonUtility.ToJson(enemy);
            var fallback=EnemyMapBalance.Resolve(enemy,null);
            check(fallback.health==enemy.Health&&fallback.experience==enemy.experienceReward,"Standalone battle changed "+enemy.name);
            var previous=EnemyMapBalance.Resolve(enemy,maps[0]);
            foreach(var map in maps.Skip(1))
            {
                var current=EnemyMapBalance.Resolve(enemy,map);
                check(current.health>0&&current.experience>0&&current.gold>0,"Invalid stats "+enemy.name);
                if(enemy.progressionRank!=EnemyProgressionRank.Tutorial&&enemy.progressionRank!=EnemyProgressionRank.LearningTrial)
                    check(current.health>previous.health&&current.experience>previous.experience&&current.gold>previous.gold,"Map curve did not increase "+enemy.name);
                previous=current;
            }
            check(original==JsonUtility.ToJson(enemy),"Shared enemy asset mutated during resolution");
        }
        var tutorial=enemies.Single(e=>e.progressionRank==EnemyProgressionRank.Tutorial);
        check(EnemyMapBalance.Resolve(tutorial,"Whispering Hollow").health==55&&EnemyMapBalance.Resolve(tutorial,"Whispering Hollow").experience==10,"Tutorial became harder");
        var sentinel=enemies.Single(e=>e.progressionRank==EnemyProgressionRank.LearningTrial);
        var final=EnemyMapBalance.Resolve(sentinel,"Forest of Dawn");
        check(final.health==190&&final.experience*2==280&&final.gold*2==100,"Two-sentinel reward mismatch");
        var boss=enemies.Single(e=>e.name=="Scene 3 Flying Demon");
        var finalBoss=enemies.Single(e=>e.name=="Forest of Dawn Final Boss");
        check(finalBoss.progressionRank==EnemyProgressionRank.Boss&&EnemyMapBalance.Resolve(finalBoss,"Forest of Dawn").health==2415,"Final boss rank or health regressed");
        check(EnemyMapBalance.Resolve(boss,"Shadowroot Depths").health>EnemyMapBalance.Resolve(enemies.Single(e=>e.name=="Forest Demon"),"Shadowroot Depths").health*2,"Boss is no stronger than normal enemy");
        var dataType=typeof(CharacterInventory).GetNestedType("SaveData",BindingFlags.NonPublic);
        var grant=typeof(CharacterInventory).GetMethod("ApplyEncounterGold",BindingFlags.NonPublic|BindingFlags.Static);
        object inventory=Activator.CreateInstance(dataType,true);
        Func<string,int,int> award=(id,gold)=>(int)grant.Invoke(null,new[]{inventory,(object)id,gold});
        check(award("Scene3/enemy",30)==30&&award("Scene3/enemy",30)==0,"Duplicate gold paid");
        check(award("Shadowroot Depths/enemy",30)==0,"Renamed map paid the same encounter twice");
        check(award("map 5/enemy",75)==75,"Another map lost its gold");
        inventory=JsonUtility.FromJson(JsonUtility.ToJson(inventory),dataType);
        check(award("Scene3/enemy",30)==0&&(int)dataType.GetField("coins").GetValue(inventory)==105,"Gold receipt lost on reload");
        check(award("invalid",0)==0&&award("invalid",-1)==0&&award(null,30)==0,"Invalid reward granted");
        dataType.GetField("coins").SetValue(inventory,int.MaxValue-2);
        check(award("cap",75)==2&&(int)dataType.GetField("coins").GetValue(inventory)==int.MaxValue,"Gold overflow or incorrect receipt amount");
        dataType.GetField("coins").SetValue(inventory,0);
        check(award("cap",75)==0,"Capped reward paid again");
        inventory=JsonUtility.FromJson("{\"coins\":7}",dataType);
        check(award("legacy",15)==15,"Old inventory cannot receive gold");
        var active=SceneManager.GetActiveScene();
        var encounterIds=new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach(var map in maps)
            {
                string path="Assets/Scenes/"+map+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var encounters=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true)).ToArray();
                    check(encounters.Length>0,"Empty map "+map);
                    foreach(var encounter in encounters)
                    {
                        check(encounterIds.Add(encounter.name),"Encounter ID reused across maps: "+encounter.name);
                        var squad=encounter.battleEncounter!=null?encounter.battleEncounter.CreateEnemySquad():new List<EnemyData>{encounter.enemy};
                        check(squad.Count>0&&squad.All(e=>e!=null),"Empty encounter "+encounter.name);
                        check(squad.Sum(e=>EnemyMapBalance.Resolve(e,map).gold)>0,"Encounter has no gold "+encounter.name);
                    }
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EnemyView.prefab");
                // Runtime clones can rearrange their HUD; connected Edit Mode prefab instances cannot.
                var go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,preview);var view=go.GetComponent<EnemyView>();
                foreach(var map in maps)
                {
                    view.SetUp(boss,map);var expected=EnemyMapBalance.Resolve(boss,map);
                    check(view.MaxHealth==expected.health&&view.CurrentHealth==expected.health&&view.ExperienceReward==expected.experience&&view.GoldReward==expected.gold,"Battle view did not apply source-map stats");
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        finally{SceneManager.SetActiveScene(active);}
        check(before==JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active)),"Player save changed");
        return count+" checks passed: map HP/XP/gold curves, actual enemy view setup, squad rewards, tutorial, boss rank, legacy inventories, duplicate receipts, reload and overflow. Player save unchanged.";
    }
}
