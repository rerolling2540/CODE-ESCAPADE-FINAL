using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine
{
    public class Coroutine { public IEnumerator routine; }
    public class GameObject { public GameObject(string name) { } public T AddComponent<T>() where T:new() {return new T();} }
    public class MonoBehaviour
    {
        public GameObject gameObject;
        public void Destroy(object value) { }
        public void DontDestroyOnLoad(object value) { }
        public Coroutine StartCoroutine(IEnumerator routine) { return new Coroutine{routine=routine}; }
        public void StopAllCoroutines() { }
        public void StopCoroutine(Coroutine routine) { }
    }
    public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float value) { } }
    public static class Debug { public static void LogWarning(object value) { } }
    public static class Application { public static string persistentDataPath="Temp/DatabaseTests/CloudSyncFixture"; }
    public static class PlayerPrefs
    {
        static Dictionary<string,string> strings=new Dictionary<string,string>();
        public static string GetString(string key,string fallback="") { return strings.ContainsKey(key)?strings[key]:fallback; }
        public static void SetString(string key,string value) { strings[key]=value; }
        public static void Save() { }
    }
}
public static class SaveSlots
{
    public const int Count=10;
    public static event Action LocalSaveChanged;
    [Serializable] public class CloudSnapshot
    {
        public int activeSlot,selectedCharacter;
        public string modifiedUtc;
        public string[] slots=new string[10],inventories=new string[10],slotModifiedUtc=new string[10];
    }
    public static CloudSnapshot Current=new CloudSnapshot();
    public static bool HasLocalProgress() { return true; }
    public static bool HasProgress(CloudSnapshot snapshot) { return snapshot!=null; }
    public static void MarkActiveProfileBootstrapComplete() { }
    public static void Change(CloudSnapshot snapshot) { Current=snapshot;LocalSaveChanged?.Invoke(); }
    public static CloudSnapshot CaptureCloudSnapshot() { return JsonUtility.FromJson<CloudSnapshot>(JsonUtility.ToJson(Current)); }
    public static void ApplyCloudSnapshot(CloudSnapshot snapshot) { Current=snapshot; }
    public static void ApplyCloudSlotModifiedUtc(int slot,string value) {Current.slotModifiedUtc[slot]=value;}
    public static string GetSlotModifiedUtc(int slot) {return Current.slotModifiedUtc[slot];}
    // Production merge is tested separately against the compiled Unity assembly.
    public static CloudSnapshot MergeCloudSnapshots(CloudSnapshot local,CloudSnapshot cloud)
    {
        if(cloud==null)return local;if(local==null)return cloud;
        var result=JsonUtility.FromJson<CloudSnapshot>(JsonUtility.ToJson(cloud));
        for(int i=0;i<10;i++)
            if(DateTime.TryParse(local.slotModifiedUtc[i],out var l)&&(!DateTime.TryParse(cloud.slotModifiedUtc[i],out var c)||l>c))
            {result.slots[i]=local.slots[i];result.inventories[i]=local.inventories[i];result.slotModifiedUtc[i]=local.slotModifiedUtc[i];}
        return result;
    }
}
public static class CharacterInventory
{
    public static event Action InventoryChanged;
    public class CloudCardStack { public string card_key;public int quantity,level; }
    public static List<CloudCardStack> ReadSavedCards(int slot) {return new List<CloudCardStack>();}
    public static void ApplyCloudCards(int slot,List<CloudCardStack> cards) { }
}
public class QuestionAttempt { public string attempt_id; }
public static class QuestionPerformanceTracker
{
    public static event Action PendingAttemptsChanged;
    public static List<QuestionAttempt> PreparePendingForUser(string id) {return new List<QuestionAttempt>();}
    public static void MarkSynced(string id,IEnumerable<string> ids) { }
    public static void MergeRemote(string id,IEnumerable<QuestionAttempt> attempts) { }
    public static void SetSyncStatus(string value) { }
}
public static class CloudSyncRegression
{
    static int checks;
    static CloudSaveSync sync;
    static void Check(bool value,string label) {if(!value)throw new Exception(label);checks++;}
    static object Call(string name,params object[] args) {return typeof(CloudSaveSync).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sync,args);}
    static bool Flag(string name) {return (bool)typeof(CloudSaveSync).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sync);}
    static void Drive(IEnumerator routine)
    {
        int limit=200;
        while(routine.MoveNext())
        {
            if(--limit==0)throw new Exception("Coroutine did not finish");
            if(routine.Current is IEnumerator)Drive((IEnumerator)routine.Current);
        }
    }
    static void Reply(long code,string body="") {UnityWebRequest.Replies.Enqueue(new UnityWebRequest.Response{code=code,body=body});}
    static SaveSlots.CloudSnapshot Snapshot(string value,string time)
    {
        var result=new SaveSlots.CloudSnapshot{modifiedUtc=time};result.slots[0]=value;result.slotModifiedUtc[0]=time;return result;
    }
    static string Result(SaveSlots.CloudSnapshot snapshot,bool conflict=false) {return "{\"save_data\":"+JsonUtility.ToJson(snapshot)+",\"conflicted_slots\":"+(conflict?"[0]":"[]")+"}";}
    static void RecoveryTick()
    {
        var recovery=(IEnumerator)Call("RecoverSession");recovery.MoveNext();
        int limit=20;
        while(recovery.MoveNext()&&!(recovery.Current is WaitForSecondsRealtime))
        {
            if(--limit==0)throw new Exception("Recovery did not yield");
            if(recovery.Current is IEnumerator)Drive((IEnumerator)recovery.Current);
        }
    }
    public static void Main()
    {
        string fresh="test."+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"exp\":"+(DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600)+"}")).TrimEnd('=')+".signature";
        MainMenuSaveSlots.CurrentUserId="user-a";MainMenuSaveSlots.CurrentAccessToken=fresh;MainMenuSaveSlots.CurrentRefreshToken="refresh";
        const string t1="2026-11-06T01:00:00Z",t2="2026-11-06T02:00:00Z",t3="2026-11-06T03:00:00Z";
        SaveSlots.Current=Snapshot("device-progress",t1);
        sync=new CloudSaveSync();Call("Awake");sync.BeginSession("https://test.invalid","key",fresh,"user-a");
        Reply(0);bool notified=false;
        Drive((IEnumerator)Call("InitialSyncRoutine",true,null,new Action<string>(value=>notified=true)));
        Check(notified&&!Flag("sessionReady"),"Initial failure releases login UI and leaves synchronization pending");
        Check(SaveSlots.Current.slots[0]=="device-progress","Initial failure preserves the local save");
        Reply(200,"[]");Reply(200,Result(SaveSlots.Current));Reply(200,"[]");Reply(200,"[]");Reply(200,"[]");
        RecoveryTick();
        Check(Flag("sessionReady"),"Recovery retries the failed initial synchronization");
        Check(sync.Status.Contains("synchronized"),"Successful recovery updates the account status");
        SaveSlots.Change(Snapshot("new-local-progress",t2));Reply(0);
        Drive((IEnumerator)Call("UploadRoutine",SaveSlots.CaptureCloudSnapshot(),null));
        Check(Flag("pendingProgress"),"Failed uploads remain queued");
        Reply(200,Result(SaveSlots.Current));RecoveryTick();
        Check(!Flag("pendingProgress"),"Recovery retries pending progress without another local change");
        var oldBranch=SaveSlots.CaptureCloudSnapshot();Reply(200,Result(oldBranch));
        var inFlight=(IEnumerator)Call("UploadRoutine",oldBranch,null);inFlight.MoveNext();
        SaveSlots.Change(Snapshot("edited-during-upload",t3));Drive((IEnumerator)inFlight.Current);Drive(inFlight);
        Check(SaveSlots.Current.slots[0]=="edited-during-upload","In-flight response cannot erase newer local edits");
        Check(Flag("pendingProgress"),"In-flight edits remain pending for the next upload");
        var stale=SaveSlots.CaptureCloudSnapshot();var account=Snapshot("account-winning-branch",t2);
        Reply(200,Result(account,true));var first=(IEnumerator)Call("UploadRoutine",stale,null);first.MoveNext();
        var queued=(IEnumerator)Call("UploadRoutine",stale,null);queued.MoveNext();
        Drive((IEnumerator)first.Current);Drive(first);
        Check(SaveSlots.Current.slots[0]=="account-winning-branch","Conflicting account branch remains active");
        Check(Directory.GetFiles(Path.Combine(Application.persistentDataPath,"CloudSaveConflicts"),"*.json").Length>0,"Conflicting device branch is backed up locally");
        Reply(200,Result(account));Drive(queued);
        Check(UnityWebRequest.LastUpload.Contains("account-winning-branch")&&!UnityWebRequest.LastUpload.Contains("edited-during-upload"),"Queued upload recaptures reconciled data instead of restoring the rejected branch");
        Check(UnityWebRequest.Replies.Count==0,"Every queued test response was consumed");
        Console.WriteLine("PASS: "+checks+" cloud recovery/in-flight/conflict checks.");
    }
}
