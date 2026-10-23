using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudSaveSync : MonoBehaviour
{
    [Serializable]
    private class SaveRow
    {
        public SaveSlots.CloudSnapshot save_data;
    }

    [Serializable]
    private class SaveRows
    {
        public SaveRow[] rows;
    }

    [Serializable]
    private class SaveUpsert
    {
        public string user_id;
        public SaveSlots.CloudSnapshot save_data;
        public string updated_at;
    }

    public static CloudSaveSync Instance { get; private set; }

    private string projectUrl;
    private string publishableKey;
    private string accessToken;
    private string userId;
    private bool sessionReady;
    private bool requestRunning;
    private bool uploadQueued;

    public static CloudSaveSync EnsureInstance()
    {
        if (Instance != null) return Instance;
        var host = new GameObject("Cloud Save Sync");
        return host.AddComponent<CloudSaveSync>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SaveSlots.LocalSaveChanged += OnLocalSaveChanged;
    }

    void OnDestroy()
    {
        SaveSlots.LocalSaveChanged -= OnLocalSaveChanged;
        if (Instance == this) Instance = null;
    }

    public void BeginSession(string url, string key, string token, string accountId)
    {
        projectUrl = url.Trim().TrimEnd('/');
        publishableKey = key.Trim();
        accessToken = token;
        userId = accountId;
        sessionReady = false;
    }

    public void EndSession()
    {
        sessionReady = false;
        projectUrl = publishableKey = accessToken = userId = "";
    }

    public void StartInitialSync(bool hasLocalProgress, Action<string> completed)
    {
        StartCoroutine(InitialSyncRoutine(hasLocalProgress, completed));
    }

    IEnumerator InitialSyncRoutine(bool hasLocalProgress, Action<string> completed)
    {
        using (var request = CreateRequest(
                   projectUrl + "/rest/v1/player_saves?user_id=eq." + userId + "&select=save_data",
                   UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke("Could not check cloud saves: " + GetError(request));
                yield break;
            }

            SaveRows rows = null;
            try
            {
                string json = request.downloadHandler.text;
                rows = JsonUtility.FromJson<SaveRows>("{\"rows\":" + json + "}");
            }
            catch (ArgumentException) { }

            SaveSlots.CloudSnapshot cloud = rows != null && rows.rows != null && rows.rows.Length > 0
                ? rows.rows[0].save_data
                : null;

            if (cloud == null)
            {
                if (hasLocalProgress)
                {
                    yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message =>
                    {
                        sessionReady = string.IsNullOrEmpty(message);
                        completed?.Invoke(sessionReady
                            ? "Guest progress uploaded to this account."
                            : "Could not upload guest progress: " + message);
                    });
                }
                else
                {
                    sessionReady = true;
                    completed?.Invoke("No cloud save yet. Progress on this device will sync to this account.");
                }
                yield break;
            }

            if (!hasLocalProgress)
            {
                SaveSlots.ApplyCloudSnapshot(cloud);
                sessionReady = true;
                completed?.Invoke("Cloud progress downloaded to this device.");
                yield break;
            }

            SaveSlots.CloudSnapshot merged = SaveSlots.MergeCloudSnapshots(
                SaveSlots.CaptureCloudSnapshot(), cloud);
            SaveSlots.ApplyCloudSnapshot(merged);
            yield return UploadRoutine(merged, message =>
            {
                sessionReady = string.IsNullOrEmpty(message);
                completed?.Invoke(sessionReady
                    ? "Device and account progress synchronized automatically."
                    : "Progress was combined locally, but cloud upload failed: " + message);
            });
            yield break;
        }
    }

    public void UploadLocalSave(Action<string> completed)
    {
        StartCoroutine(UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message =>
        {
            sessionReady = string.IsNullOrEmpty(message);
            completed?.Invoke(message);
        }));
    }

    void OnLocalSaveChanged()
    {
        if (!sessionReady || uploadQueued) return;
        uploadQueued = true;
        StartCoroutine(UploadAfterDelay());
    }

    IEnumerator UploadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        uploadQueued = false;
        if (!sessionReady) yield break;
        if (requestRunning)
        {
            uploadQueued = true;
            StartCoroutine(UploadAfterDelay());
            yield break;
        }
        yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message =>
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning("Cloud save sync failed: " + message);
        });
    }

    IEnumerator UploadRoutine(SaveSlots.CloudSnapshot snapshot, Action<string> completed)
    {
        if (requestRunning) yield break;
        requestRunning = true;

        var payload = new SaveUpsert
        {
            user_id = userId,
            save_data = snapshot,
            updated_at = DateTime.UtcNow.ToString("o")
        };

        using (var request = CreateRequest(
                   projectUrl + "/rest/v1/player_saves?on_conflict=user_id",
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");
            yield return request.SendWebRequest();
            requestRunning = false;
            completed?.Invoke(request.result == UnityWebRequest.Result.Success ? "" : GetError(request));
        }
    }

    UnityWebRequest CreateRequest(string url, string method)
    {
        var request = new UnityWebRequest(url, method);
        request.SetRequestHeader("apikey", publishableKey);
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        request.SetRequestHeader("Accept", "application/json");
        return request;
    }

    static string GetError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
        return string.IsNullOrWhiteSpace(body) ? request.error : body;
    }
}
