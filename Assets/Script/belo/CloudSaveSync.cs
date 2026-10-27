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

    [Serializable] private class InventoryStateRow { public int slot_index; public string updated_at; }
    [Serializable] private class InventoryStateRows { public InventoryStateRow[] rows; }
    [Serializable] private class InventoryCardRow
    {
        public int slot_index;
        public string card_key;
        public int quantity;
        public int level;
    }
    [Serializable] private class InventoryCardRows { public InventoryCardRow[] rows; }
    [Serializable] private class InventoryRpcCard
    {
        public string card_key;
        public int quantity;
        public int level;
    }
    [Serializable] private class InventoryRpcPayload
    {
        public int p_slot_index;
        public string p_updated_at;
        public InventoryRpcCard[] p_cards;
    }

    public static CloudSaveSync Instance { get; private set; }

    private string projectUrl;
    private string publishableKey;
    private string accessToken;
    private string userId;
    private bool sessionReady;
    private bool requestRunning;
    private bool uploadQueued;
    private bool inventorySyncRunning;
    private bool inventoryChangeSyncQueued;

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
        CharacterInventory.InventoryChanged += OnInventoryChanged;
    }

    void OnDestroy()
    {
        SaveSlots.LocalSaveChanged -= OnLocalSaveChanged;
        CharacterInventory.InventoryChanged -= OnInventoryChanged;
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
                        StartCoroutine(FinishInitialSync(sessionReady
                            ? "Guest progress uploaded to this account."
                            : "Could not upload guest progress: " + message, completed));
                    });
                }
                else
                {
                    sessionReady = true;
                    yield return FinishInitialSync("No cloud save yet. Progress on this device will sync to this account.", completed);
                }
                yield break;
            }

            if (!hasLocalProgress)
            {
                SaveSlots.ApplyCloudSnapshot(cloud);
                sessionReady = true;
                yield return FinishInitialSync("Cloud progress downloaded to this device.", completed);
                yield break;
            }

            SaveSlots.CloudSnapshot merged = SaveSlots.MergeCloudSnapshots(
                SaveSlots.CaptureCloudSnapshot(), cloud);
            SaveSlots.ApplyCloudSnapshot(merged);
            yield return UploadRoutine(merged, message =>
            {
                sessionReady = string.IsNullOrEmpty(message);
                StartCoroutine(FinishInitialSync(sessionReady
                    ? "Device and account progress synchronized automatically."
                    : "Progress was combined locally, but cloud upload failed: " + message, completed));
            });
            yield break;
        }
    }

    public void UploadLocalSave(Action<string> completed)
    {
        StartCoroutine(UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message =>
        {
            sessionReady = string.IsNullOrEmpty(message);
            if (sessionReady) StartCoroutine(FinishInitialSync("", completed));
            else completed?.Invoke(message);
        }));
    }

    IEnumerator FinishInitialSync(string status, Action<string> completed)
    {
        string inventoryWarning = "";
        if (sessionReady) yield return SyncCardInventoryRoutine(value => inventoryWarning = value);
        if (!string.IsNullOrEmpty(inventoryWarning))
            Debug.LogWarning(inventoryWarning);
        completed?.Invoke(string.IsNullOrEmpty(status) ? inventoryWarning :
            status + (string.IsNullOrEmpty(inventoryWarning) ? "" : "\n" + inventoryWarning));
    }

    void OnLocalSaveChanged()
    {
        if (!sessionReady || uploadQueued) return;
        uploadQueued = true;
        StartCoroutine(UploadAfterDelay());
    }

    void OnInventoryChanged()
    {
        if (!sessionReady || inventoryChangeSyncQueued) return;
        inventoryChangeSyncQueued = true;
        StartCoroutine(SyncInventoryAfterLocalChange());
    }

    IEnumerator SyncInventoryAfterLocalChange()
    {
        while (inventorySyncRunning && sessionReady) yield return null;
        inventoryChangeSyncQueued = false;
        if (!sessionReady) yield break;
        yield return SyncCardInventoryRoutine(message =>
        {
            if (!string.IsNullOrEmpty(message)) Debug.LogWarning("Cloud card inventory sync failed: " + message);
        });
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
        if (sessionReady)
            yield return SyncCardInventoryRoutine(message =>
            {
                if (!string.IsNullOrEmpty(message)) Debug.LogWarning(message);
            });
    }

    IEnumerator SyncCardInventoryRoutine(Action<string> completed)
    {
        while (inventorySyncRunning) yield return null;
        inventorySyncRunning = true;
        string statesUrl = projectUrl + "/rest/v1/player_card_inventory_slots?user_id=eq." +
                           UnityWebRequest.EscapeURL(userId) + "&select=slot_index,updated_at";
        InventoryStateRows stateRows = null;
        using (var request = CreateRequest(statesUrl, UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                inventorySyncRunning = false;
                completed?.Invoke("Card inventory table is not ready yet. Run card_inventory_schema.sql in Supabase. " + GetError(request));
                yield break;
            }
            try { stateRows = JsonUtility.FromJson<InventoryStateRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
            catch (ArgumentException) { }
        }

        InventoryCardRows cardRows = null;
        using (var request = CreateRequest(projectUrl + "/rest/v1/player_card_inventory?user_id=eq." +
                   UnityWebRequest.EscapeURL(userId) + "&select=slot_index,card_key:game_card_key,quantity,level", UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                inventorySyncRunning = false;
                completed?.Invoke("Could not read owned card rows: " + GetError(request));
                yield break;
            }
            try { cardRows = JsonUtility.FromJson<InventoryCardRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
            catch (ArgumentException) { }
        }

        bool appliedRemote = false;
        for (int slot = 0; slot < SaveSlots.Count; slot++)
        {
            InventoryStateRow state = stateRows?.rows != null
                ? Array.Find(stateRows.rows, row => row != null && row.slot_index == slot) : null;
            if (state == null || string.IsNullOrWhiteSpace(state.updated_at) || !IsCloudNewer(state.updated_at, SaveSlots.GetSlotModifiedUtc(slot))) continue;

            var remoteCards = new System.Collections.Generic.List<CharacterInventory.CloudCardStack>();
            if (cardRows?.rows != null)
                foreach (var row in cardRows.rows)
                    if (row != null && row.slot_index == slot)
                        remoteCards.Add(new CharacterInventory.CloudCardStack { card_key = row.card_key, quantity = row.quantity, level = row.level });
            CharacterInventory.ApplyCloudCards(slot, remoteCards);
            SaveSlots.ApplyCloudSlotModifiedUtc(slot, state.updated_at);
            appliedRemote = true;
        }

        for (int slot = 0; slot < SaveSlots.Count; slot++)
        {
            var localCards = CharacterInventory.ReadSavedCards(slot);
            var payload = new InventoryRpcPayload
            {
                p_slot_index = slot,
                p_updated_at = string.IsNullOrWhiteSpace(SaveSlots.GetSlotModifiedUtc(slot))
                    ? null : SaveSlots.GetSlotModifiedUtc(slot),
                p_cards = localCards.ConvertAll(card => new InventoryRpcCard
                    { card_key = card.card_key, quantity = card.quantity, level = card.level }).ToArray()
            };
            using (var request = CreateRequest(projectUrl + "/rest/v1/rpc/replace_player_card_inventory", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    inventorySyncRunning = false;
                    completed?.Invoke("Could not sync owned cards for save slot " + (slot + 1) + ": " + GetError(request));
                    yield break;
                }
            }
        }

        if (appliedRemote)
        {
            string saveError = null;
            yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message => saveError = message);
            if (!string.IsNullOrEmpty(saveError))
            {
                inventorySyncRunning = false;
                completed?.Invoke("Owned cards downloaded, but the combined save snapshot could not be updated: " + saveError);
                yield break;
            }
        }
        inventorySyncRunning = false;
        completed?.Invoke("");
    }

    static bool IsCloudNewer(string cloud, string local)
    {
        DateTime cloudTime, localTime;
        bool hasCloud = DateTime.TryParse(cloud, null, System.Globalization.DateTimeStyles.AssumeUniversal, out cloudTime);
        bool hasLocal = DateTime.TryParse(local, null, System.Globalization.DateTimeStyles.AssumeUniversal, out localTime);
        if (!hasCloud) return false;
        return !hasLocal || cloudTime.ToUniversalTime() > localTime.ToUniversalTime();
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
