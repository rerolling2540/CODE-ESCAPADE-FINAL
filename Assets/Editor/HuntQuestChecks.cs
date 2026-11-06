using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HuntQuestChecks
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Use Edit Mode for isolated contract checks.");
        int checks=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        string savedBefore=JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active));
        var save=new SaveSlots.Record{level=5};
        for(int giver=0;giver<2;giver++)
        {
            check(HuntQuestProgress.Stage(save,giver)==0&&!HuntQuestProgress.Ready(save,giver),"New quest auto-completed");
            check(!HuntQuestProgress.ApplyReward(save,giver,0),"Unaccepted quest paid");
            check(!HuntQuestProgress.ApplyAccept(save,giver,1),"Skipped first contract");
            for(int stage=0;stage<3;stage++)
            {
                var c=HuntQuestProgress.Get(giver,stage);
                check(HuntQuestProgress.ApplyAccept(save,giver,stage),"Unable to accept next contract");
                check(!HuntQuestProgress.ApplyAccept(save,giver,stage),"Accepted twice");
                check(!HuntQuestProgress.ApplyReward(save,giver,stage),"Unfinished contract paid");
                save.defeatedEncounters.Add("unrelated enemy");save.defeatedEncounters.Add("Tutorial Training Goblin");
                check(HuntQuestProgress.Count(save,giver,stage)==0,"Unrelated/training target counted");
                save.scene3VisitDefeated=c.encounterIds.ToList();
                check(!HuntQuestProgress.Ready(save,giver),"Visit-only kills completed a permanent contract");
                save.defeatedEncounters.Add(c.encounterIds[0]);save.defeatedEncounters.Add(c.encounterIds[0]);
                check(HuntQuestProgress.Count(save,giver,stage)==1,"Repeated enemy counted twice");
                foreach(string id in c.encounterIds.Take(c.required))save.defeatedEncounters.Add(id);
                check(HuntQuestProgress.Ready(save,giver),"Victories did not complete contract");
                SceneVisitProgress.ResetVisit(save,"Shadowroot Depths");
                check(HuntQuestProgress.Ready(save,giver),"Map revisit reset contract progress");
                check(QuestReturnPanel.Ready(save).Any(d=>d.giver==giver+3&&d.key==HuntQuestProgress.Key(giver,stage)),"Missing NPC return destination");
                check(HuntQuestProgress.ApplyReward(save,giver,stage),"Earned reward rejected");
                int xp=save.experience,level=save.level;
                check(!HuntQuestProgress.ApplyReward(save,giver,stage)&&xp==save.experience&&level==save.level,"Duplicate XP paid");
                check(HuntQuestProgress.Stage(save,giver)==stage+1,"Contract did not advance");
            }
            check(!HuntQuestProgress.Ready(save,giver)&&!HuntQuestProgress.ApplyAccept(save,giver,3),"Finished quest offered again");
        }
        check(save.level==8&&save.experience==130,"Six-contract XP total mismatch");
        var copy=JsonUtility.FromJson<SaveSlots.Record>(JsonUtility.ToJson(save));
        check(HuntQuestProgress.Stage(copy,0)==3&&HuntQuestProgress.Stage(copy,1)==3,"Contract receipts lost on save reload");
        var old=JsonUtility.FromJson<SaveSlots.Record>("{\"level\":6,\"elfQuestStage\":2}");
        check(HuntQuestProgress.Stage(old,0)==0&&old.elfQuestStage==2,"Legacy save changed");
        var earlier=new SaveSlots.Record();earlier.defeatedEncounters.AddRange(HuntQuestProgress.Get(0,0).encounterIds);
        check(!HuntQuestProgress.Ready(earlier,0),"Earlier victories skipped acceptance");HuntQuestProgress.ApplyAccept(earlier,0,0);
        check(HuntQuestProgress.Ready(earlier,0),"Earlier victories did not count");
        var renamed=new SaveSlots.Record();renamed.defeatedEncounters.AddRange(new[]{"boss","boss","Scene 2 - Bringer Of Death 01"});
        check(HuntQuestProgress.Count(renamed,0,2)==1,"Renamed Bringer victory lost or counted twice");
        check(HuntQuestProgress.Count(renamed,1,2)==0,"Bringer incorrectly completed Joanna's boss contract");
        check(!HuntQuestProgress.Ready(new SaveSlots.Record(),0),"Progress leaked between slots");
        var dataType=typeof(CharacterInventory).GetNestedType("SaveData",BindingFlags.NonPublic);var data=Activator.CreateInstance(dataType,true);
        var grant=typeof(CharacterInventory).GetMethod("ApplyHuntGrant",BindingFlags.NonPublic|BindingFlags.Static);
        for(int giver=0;giver<2;giver++)for(int stage=0;stage<3;stage++)
        {
            var c=HuntQuestProgress.Get(giver,stage);var args=new object[]{data,HuntQuestProgress.Key(giver,stage),c.gold,c.scraps,c.tokens};
            check((bool)grant.Invoke(null,args),"Inventory reward rejected");
            check(!(bool)grant.Invoke(null,args),"Duplicate gold/materials paid");
        }
        check((int)dataType.GetField("coins").GetValue(data)==1175,"Wrong total gold");
        check((int)dataType.GetField("lowLogicScraps").GetValue(data)==12&&(int)dataType.GetField("libraryTokens").GetValue(data)==3,"Wrong materials/tokens");
        dataType.GetField("coins").SetValue(data,int.MaxValue);grant.Invoke(null,new object[]{data,"overflow-test",100,0,0});
        check((int)dataType.GetField("coins").GetValue(data)==int.MaxValue,"Gold overflow");
        var active=SceneManager.GetActiveScene();
        try
        {
            for(int giver=0;giver<2;giver++)
            {
                var path="Assets/Scenes/"+HuntQuestProgress.Scene(giver)+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var hunts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HuntQuestNPC>(true)).ToArray();
                    check(hunts.Length==1&&hunts[0].giver==giver,"Missing/duplicate/wrong-map NPC");var npc=hunts[0];
                    check(PrefabUtility.IsPartOfPrefabInstance(npc),"NPC is not selectable prefab instance");
                    var talk=npc.GetComponent<Npc_Talk>();check(talk!=null&&!talk.offersElfQuest&&talk.actor.actorName==HuntQuestProgress.Name(giver),"Wrong dialogue identity");
                    check(npc.GetComponentsInChildren<Canvas>(true).Length==0,"NPC has a separate dialogue canvas");
                    check(npc.GetComponent<CircleCollider2D>().isTrigger,"NPC blocks the player");
                    var animation=npc.GetComponentInChildren<NPCSpriteAnimation>();check(animation!=null&&animation.frames.Length==(giver==0?9:6)&&animation.frames.All(f=>f!=null),"Missing idle frames");
                    check(animation.GetComponent<SpriteRenderer>().sortingOrder<16,"NPC draws over the warrior");
                    var ids=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveEnemyEncounter>(true)).Select(e=>e.name).ToArray();
                    for(int stage=0;stage<3;stage++)foreach(string id in HuntQuestProgress.Get(giver,stage).encounterIds)check(ids.Count(n=>n==id)==1,"Missing or ambiguous target: "+id);
                    SceneManager.SetActiveScene(scene);check(QuestReturnPanel.FindNpc(giver+3)==talk,"Return travel resolved wrong NPC");
                    Physics2D.SyncTransforms();check(Physics2D.RaycastAll((Vector2)npc.transform.position+Vector2.up*.2f,Vector2.down,.6f).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.gameObject.scene==scene&&h.normal.y>.5f),"NPC is floating without ground");
                }
                finally{SceneManager.SetActiveScene(active);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            Preview(check);
        }
        finally{SceneManager.SetActiveScene(active);}
        check(savedBefore==JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active)),"Checks changed player save");
        return checks+" combat quest checks passed: six stages, target validation, unique victories, legacy/slot persistence, respawns, duplicate XP/inventory claims, scene placement, shared dialogue, NPC travel lookup and seven-reward scrolling. No player saves changed.";
    }
    static void Preview(Action<bool,string> check)
    {
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var s=new SaveSlots.Record{wardenAccepted=true,wardenChapter=4,elfQuestStage=1};
            for(int t=0;t<6;t++)s.workshopExercisesCompleted.Add(WorkshopProgression.Id(LearningQuests.Language,t,0));
            for(int giver=0;giver<3;giver++)s.learningQuestsAccepted.Add(LearningQuests.Key(giver));
            s.wardenProofs.AddRange(new[]{WardenQuest.Proof("shelf"),WardenQuest.Proof("heal")});
            for(int giver=0;giver<2;giver++){HuntQuestProgress.ApplyAccept(s,giver,0);s.defeatedEncounters.AddRange(HuntQuestProgress.Get(giver,0).encounterIds);}
            var destinations=QuestReturnPanel.Ready(s);check(destinations.Count==7,"Expected seven simultaneous reward destinations");
            var controller=new GameObject("Reward preview",typeof(QuestReturnPanel)).GetComponent<QuestReturnPanel>();var root=controller.BuildPreview(destinations);
            Capture(root,"Hollow-Contracts-Rewards.png",check);
            var scroll=root.GetComponentInChildren<UnityEngine.UI.ScrollRect>();check(scroll.content.rect.height>scroll.viewport.rect.height,"Seven rewards did not scroll");
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();check(scroll.content.anchoredPosition.y>0,"Cannot scroll to Joanna reward");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(controller.gameObject);
            for(int giver=0;giver<2;giver++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HuntQuestAuthoring.Folder+"/"+HuntQuestProgress.Name(giver)+" - Hollow Contracts.prefab");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.position=new Vector3(giver==0?-2:2,0,0);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            }
            var cam=new GameObject("NPC preview camera",typeof(Camera)).GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.8f;cam.transform.position=new Vector3(0,1.4f,-10);cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.15f,.19f);
            WriteCamera(cam,"Eleonore-Joanna-Preview.png");
        }
        finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
    }
    static void Capture(GameObject root,string name,Action<bool,string> check)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        var camera=new GameObject("Panel camera",typeof(Camera)).GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        WriteCamera(camera,name,()=>{Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>()){t.ForceMeshUpdate();check(!t.isTextOverflowing,"Overflowing contract UI label: "+t.text);}});
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
    static void WriteCamera(Camera camera,string name,Action before=null)
    {
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;var old=RenderTexture.active;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{before?.Invoke();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("C:/Users/BURAT/.codex/visualizations/2026/10/10/01a12675-f56a-7032-8484-45c9ba6483bc/"+name,tex.EncodeToPNG());}
        finally{RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
    }
}
