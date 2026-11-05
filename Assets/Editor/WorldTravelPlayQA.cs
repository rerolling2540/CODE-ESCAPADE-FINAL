using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit editor smoke test with scoped save/profile backup and automatic restoration on stop.</summary>
[InitializeOnLoad]
public static class WorldTravelPlayQA
{
    [Serializable] class Pref {public string key,value;public bool integer;public int number;}
    [Serializable] class Backup {public List<Pref> prefs=new List<Pref>();}
    const string Token="WorldTravel.PlayQA.";
    static WorldTravelPlayQA(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Monitor;}
    static bool Scope(string key)=>key=="SelectedCharacter"||key.StartsWith("CodeEscapade.")&&!key.StartsWith("CodeEscapade.Minimap.");
    static IEnumerable<string> Names()
    {
        var names=new HashSet<string>();
        foreach(string path in new[]{"Software\\Unity\\UnityEditor\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName,"Software\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName})
        using(var registry=Registry.CurrentUser.OpenSubKey(path))if(registry!=null)foreach(string raw in registry.GetValueNames()){string key=Regex.Replace(raw,@"_h\d+$","");var kind=registry.GetValueKind(raw);if((kind==RegistryValueKind.String||kind==RegistryValueKind.DWord||kind==RegistryValueKind.Binary)&&Scope(key)&&PlayerPrefs.HasKey(key))names.Add(key);}
        return names;
    }
    static bool Integer(string key)
    {
        using(var registry=Registry.CurrentUser.OpenSubKey("Software\\Unity\\UnityEditor\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName))
            if(registry!=null)foreach(string raw in registry.GetValueNames())if(Regex.Replace(raw,@"_h\d+$","")==key)return registry.GetValueKind(raw)==RegistryValueKind.DWord;
        return key=="SelectedCharacter"||key=="CodeEscapade.ActiveSlot"||key.Contains("Migration")||key.Contains("Bootstrap");
    }
    public static string Prepare()
    {
        if(EditorApplication.isPlaying||SessionState.GetBool(Token+"armed",false))throw new Exception("A play test is already active.");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene authoring before starting a play test.");
        var backup=new Backup();foreach(var name in Names())
        {
            bool integer=Integer(name);
            backup.prefs.Add(new Pref{key=name,integer=integer,number=integer?PlayerPrefs.GetInt(name):0,value=integer?null:PlayerPrefs.GetString(name)});
        }
        if(!backup.prefs.Any(p=>p.key.StartsWith("CodeEscapade.Slot.")))throw new Exception("Could not locate save preferences for backup; play test was not started.");
        SessionState.SetString(Token+"backup",JsonUtility.ToJson(backup));SessionState.SetString(Token+"scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Token+"armed",true);
        foreach(var p in backup.prefs)PlayerPrefs.DeleteKey(p.key);PlayerPrefs.SetInt("CodeEscapade.ActiveSlot",9);PlayerPrefs.SetInt("SelectedCharacter",0);PlayerPrefs.SetInt("CodeEscapade.SlotMigration.v1",1);PlayerPrefs.Save();
        SessionState.SetString(Token+"pending","Shadowroot Depths");SessionState.SetBool(Token+"arrival",false);SessionState.EraseString(Token+"error");
        EditorSceneManager.OpenScene("Assets/Scenes/Shadowroot Depths.unity",OpenSceneMode.Single);return "Prepared isolated travel test. Existing save/profile preferences are backed up and restore automatically when Play Mode stops.";
    }
    static void Changed(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Token+"armed",false))EditorApplication.delayCall+=Restore;}
    static void Monitor()
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool(Token+"armed",false)||Time.frameCount<3)return;
        string expected=SessionState.GetString(Token+"pending","");
        if(string.IsNullOrEmpty(expected)||SceneManager.GetActiveScene().name!=expected||SceneTransitionFader.IsTransitioning)return;
        if(UnityEngine.Object.FindFirstObjectByType<MonolithTravel>()==null)return;
        try
        {
            string arrival=SessionState.GetBool(Token+"arrival",false)?CheckArrival(expected):"Initial player loop advanced to frame "+Time.frameCount;
            MoveToPortal();string opened=OpenPortal();SessionState.SetString(Token+"result",arrival+"\n"+opened);
        }
        catch(Exception e){SessionState.SetString(Token+"error",e.Message);Time.timeScale=0;}
        SessionState.SetString(Token+"pending","");
    }
    public static string Result()=>SessionState.GetString(Token+"error","")+SessionState.GetString(Token+"result","");
    public static void Restore()
    {
        if(EditorApplication.isPlaying||!SessionState.GetBool(Token+"armed",false))return;
        var backup=JsonUtility.FromJson<Backup>(SessionState.GetString(Token+"backup",""));foreach(var name in Names())PlayerPrefs.DeleteKey(name);
        foreach(var p in backup.prefs)if(p.integer)PlayerPrefs.SetInt(p.key,p.number);else PlayerPrefs.SetString(p.key,p.value);PlayerPrefs.Save();
        bool verified=backup.prefs.All(p=>p.integer?PlayerPrefs.GetInt(p.key)==p.number:PlayerPrefs.GetString(p.key)==p.value);
        SessionState.SetBool(Token+"restoredVerified",verified);
        if(!verified)throw new Exception("Travel QA save restoration did not verify.");
        SessionState.SetBool(Token+"armed",false);SessionState.EraseString(Token+"backup");var scene=SessionState.GetString(Token+"scene","");if(!string.IsNullOrEmpty(scene)&&SceneManager.GetActiveScene().path!=scene)EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
        Debug.Log("World travel QA finished. Original save/profile preferences and editor scene restored.");
    }
    public static string MoveToPortal()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode first.");Time.timeScale=1;
        var monolith=UnityEngine.Object.FindObjectsByType<MonolithTravel>(FindObjectsSortMode.None).First(m=>m.GetComponentInChildren<PortalArrivalPoint>()!=null);
        var player=UnityEngine.Object.FindFirstObjectByType<playerMovement>();var collider=player.GetComponent<Collider2D>();float foot=collider.bounds.min.y-player.transform.position.y;
        Vector3 safe;if(!SafeCheckpoint.TryFind(player,monolith.transform.position+Vector3.up*(-foot+.08f),out safe)||Vector2.Distance(safe,monolith.transform.position)>4)throw new Exception("No safe portal approach.");
        var delta=safe-player.transform.position;player.transform.position=safe;var body=player.GetComponent<Rigidbody2D>();body.position=safe;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();foreach(var c in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))if(c.Follow==player.transform)c.OnTargetObjectWarped(player.transform,delta);
        SessionState.SetInt(Token+"frame",Time.frameCount);return "Moved test player to "+monolith.name+" at "+safe+". Frame "+Time.frameCount;
    }
    public static string OpenPortal()
    {
        if(Time.frameCount<3)throw new Exception("Player loop did not advance.");
        var portal=UnityEngine.Object.FindObjectsByType<MonolithTravel>(FindObjectsSortMode.None).First(m=>m.GetComponentInChildren<PortalArrivalPoint>()!=null).GetComponent<PortalInteraction>();
        var player=UnityEngine.Object.FindFirstObjectByType<playerMovement>();var collider=player.GetComponent<Collider2D>();
        if(!Physics2D.Distance(portal.GetComponent<Collider2D>(),collider).isOverlapped)throw new Exception("Test player is outside the monolith trigger.");
        typeof(PortalInteraction).GetMethod("Track",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(portal,new object[]{collider});
        typeof(PortalInteraction).GetMethod("TryEnter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(portal,null);
        if(!MonolithTravelPanel.IsOpen)throw new Exception("Portal interaction did not open travel panel.");
        Canvas.ForceUpdateCanvases();int overflow=UnityEngine.Object.FindFirstObjectByType<MonolithTravelPanel>().GetComponentsInChildren<TMPro.TMP_Text>().Count(t=>{t.ForceMeshUpdate();return t.isTextOverflowing;});
        if(overflow>0)throw new Exception("Travel labels overflow: "+overflow);
        return "Portal interaction opened destination panel; timeScale="+Time.timeScale+", frame="+Time.frameCount+", no overflowing labels.";
    }
    public static string Travel(string scene)
    {
        var panel=UnityEngine.Object.FindFirstObjectByType<MonolithTravelPanel>();if(panel==null)throw new Exception("No open travel panel.");SessionState.SetString(Token+"pending",scene);SessionState.SetBool(Token+"arrival",true);panel.Travel(WorldTravelCatalog.Load().destinations.First(d=>d.sceneName==scene));return "Requested travel to "+scene;
    }
    public static string CheckArrival(string expected)
    {
        if(SceneManager.GetActiveScene().name!=expected||SceneTransitionFader.IsTransitioning)throw new Exception("Travel transition has not completed.");
        var player=UnityEngine.Object.FindFirstObjectByType<playerMovement>();var arrival=UnityEngine.Object.FindObjectsByType<PortalArrivalPoint>(FindObjectsSortMode.None).Single(a=>a.arrivalId==WorldTravelCatalog.ArrivalId);float distance=Vector2.Distance(player.transform.position,arrival.transform.position);
        if(distance>8||!SafeCheckpoint.IsSafe(player,player.transform.position))throw new Exception("Unsafe or distant monolith arrival: "+distance);
        return "Arrived in "+expected+" on safe ground, "+distance.ToString("0.0")+" units from the destination anchor. timeScale="+Time.timeScale;
    }
}

