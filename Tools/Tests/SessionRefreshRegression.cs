using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

// Test doubles exercise the actual SupabaseSession.cs coroutine implementation.
// No passwords, tokens, HTTP requests or Unity scenes from the real project are used.
namespace UnityEngine
{
    public static class JsonUtility
    {
        public static string ToJson(object value) { return new JavaScriptSerializer().Serialize(value); }
        public static T FromJson<T>(string json) { return (T)ConvertObject(typeof(T),new JavaScriptSerializer().DeserializeObject(json)); }
        static object ConvertObject(Type type, object value)
        {
            if (value == null) return null;
            if (type.IsArray)
            {
                var source = (object[])value;
                var array = Array.CreateInstance(type.GetElementType(),source.Length);
                for(int i=0;i<source.Length;i++)array.SetValue(ConvertObject(type.GetElementType(),source[i]),i);
                return array;
            }
            if (!(value is Dictionary<string,object>)) return Convert.ChangeType(value,type);
            var result = Activator.CreateInstance(type,true);
            var values = (Dictionary<string,object>)value;
            foreach (var field in type.GetFields())
                if (values.ContainsKey(field.Name)) field.SetValue(result,ConvertObject(field.FieldType,values[field.Name]));
            return result;
        }
    }
}
namespace UnityEngine.Networking
{
    public class DownloadHandlerBuffer { public string text; }
    public class UploadHandlerRaw { public byte[] data; public UploadHandlerRaw(byte[] value) { data=value; } }
    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress,Success,ConnectionError,ProtocolError }
        public sealed class Response { public long code; public string body; }
        public static Queue<Response> Replies = new Queue<Response>();
        public static int Sent;
        public static string LastUpload;
        public const string kHttpVerbGET="GET",kHttpVerbPOST="POST";
        public string error="Offline test response";
        public static string EscapeURL(string value) { return Uri.EscapeDataString(value); }
        public int timeout;
        public UploadHandlerRaw uploadHandler;
        public DownloadHandlerBuffer downloadHandler;
        public Result result;
        public long responseCode;
        public Dictionary<string,string> headers = new Dictionary<string,string>();
        public UnityWebRequest(string url,string method) { }
        public void SetRequestHeader(string name,string value) { headers[name]=value; }
        public object SendWebRequest()
        {
            Sent++;
            LastUpload=uploadHandler!=null?Encoding.UTF8.GetString(uploadHandler.data):null;
            var reply=Replies.Dequeue();
            responseCode=reply.code;
            result=reply.code==200 ? Result.Success : reply.code==0 ? Result.ConnectionError : Result.ProtocolError;
            if(downloadHandler!=null)downloadHandler.text=reply.body;
            return null;
        }
        public void Dispose() { }
        public void Abort() { result=Result.ConnectionError; }
    }
}
public static class MainMenuSaveSlots
{
    public static string CurrentAccessToken,CurrentRefreshToken,CurrentUserId;
    public static string CurrentSupabaseProjectUrl="https://test.invalid",CurrentSupabasePublishableKey="test-key";
    public static bool AcceptRefreshedSession(string id,string previous,string token,string refresh)
    {
        if(id!=CurrentUserId||previous!=CurrentAccessToken)return false;
        CurrentAccessToken=token;CurrentRefreshToken=refresh;return true;
    }
}
public static class SessionRefreshRegression
{
    static int checks;
    static void Check(bool value,string message) { if(!value)throw new Exception(message);checks++; }
    static void Drive(IEnumerator routine)
    {
        int limit=100;
        while(routine.MoveNext())
        {
            if(--limit==0)throw new Exception("Coroutine did not finish");
            if(routine.Current is IEnumerator)Drive((IEnumerator)routine.Current);
        }
    }
    static string Token(long expiry) { return "test."+Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"exp\":"+expiry+"}")).TrimEnd('=').Replace('+','-').Replace('/','_')+".signature"; }
    static void Login(string token)
    {
        MainMenuSaveSlots.CurrentUserId="user-a";MainMenuSaveSlots.CurrentAccessToken=token;MainMenuSaveSlots.CurrentRefreshToken="refresh-old";
    }
    static void Reply(long code,string body="") { UnityEngine.Networking.UnityWebRequest.Replies.Enqueue(new UnityEngine.Networking.UnityWebRequest.Response{code=code,body=body}); }
    static string Response(string token) { return "{\"access_token\":\""+token+"\",\"refresh_token\":\"refresh-new\",\"user\":{\"id\":\"user-a\"}}"; }
    public static void Main()
    {
        long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string expired=Token(now-10),fresh=Token(now+3600);
        Check(SupabaseSession.ExpiryUtc(fresh)>DateTime.UtcNow.AddMinutes(50),"JWT expiry decoding");
        Check(SupabaseSession.ExpiryUtc("invalid")==DateTime.MinValue,"Malformed JWT expires safely");
        Login(fresh);bool ready=false;Drive(SupabaseSession.EnsureAccessToken(value=>ready=value));
        Check(ready&&UnityEngine.Networking.UnityWebRequest.Sent==0,"Fresh sessions do not refresh");
        Login(expired);Reply(200,Response(fresh));Drive(SupabaseSession.EnsureAccessToken(value=>ready=value));
        Check(ready&&MainMenuSaveSlots.CurrentAccessToken==fresh,"Expired session refresh succeeds");
        Check(MainMenuSaveSlots.CurrentRefreshToken=="refresh-new","Rotated refresh token is retained");
        Login(expired);Reply(200,Response(fresh));int before=UnityEngine.Networking.UnityWebRequest.Sent;
        var first=SupabaseSession.EnsureAccessToken(value=>ready=value);first.MoveNext();
        bool secondReady=false;var second=SupabaseSession.EnsureAccessToken(value=>secondReady=value);second.MoveNext();
        Drive(first);Drive(second);
        Check(ready&&secondReady&&UnityEngine.Networking.UnityWebRequest.Sent==before+1,"Concurrent requests share one refresh");
        Login(expired);Reply(0);Drive(SupabaseSession.EnsureAccessToken(value=>ready=value));
        Check(!ready&&MainMenuSaveSlots.CurrentAccessToken==expired,"Offline refresh preserves local session");
        Reply(200,Response(fresh));Drive(SupabaseSession.EnsureAccessToken(value=>ready=value));
        Check(ready,"Refresh retries after connectivity returns");
        Login(expired);Reply(200,Response(fresh));var switching=SupabaseSession.EnsureAccessToken(value=>ready=value);switching.MoveNext();
        MainMenuSaveSlots.CurrentUserId="user-b";MainMenuSaveSlots.CurrentAccessToken="different-session";Drive(switching);
        Check(!ready&&MainMenuSaveSlots.CurrentAccessToken=="different-session","Late refresh cannot restore a logged-out account");
        Login(fresh);Reply(401);var request=new UnityEngine.Networking.UnityWebRequest("https://test.invalid/rest/v1/cards","GET");
        Drive(SupabaseSession.Send(request));
        Check(request.headers["Authorization"]=="Bearer "+fresh,"Requests use the shared current token");
        Reply(200,Response(Token(now+7200)));Drive(SupabaseSession.EnsureAccessToken(value=>ready=value));
        Check(ready&&MainMenuSaveSlots.CurrentAccessToken!=fresh,"401 forces refresh before retry");
        Login(expired);Reply(200,Response(fresh));var cancelled=SupabaseSession.EnsureAccessToken(value=>ready=value);cancelled.MoveNext();
        SupabaseSession.CancelPendingRefresh();
        Reply(200,Response(Token(now+7200)));var replacement=SupabaseSession.EnsureAccessToken(value=>ready=value);replacement.MoveNext();
        Drive(cancelled);
        Check(MainMenuSaveSlots.CurrentAccessToken==expired,"Cancelled refresh cannot publish its response");
        Drive(replacement);
        Check(ready&&MainMenuSaveSlots.CurrentAccessToken!=expired,"New session can refresh after cancellation");
        Console.WriteLine("PASS: "+checks+" session refresh checks.");
    }
}
