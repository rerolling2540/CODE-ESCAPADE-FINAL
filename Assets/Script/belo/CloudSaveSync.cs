using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudSaveSync : MonoBehaviour
{
    [Serializable]
    private class SaveRow
    {
        public SaveSlots.CloudSnapshot save_data;
        public int[] conflicted_slots;
    }

    [Serializable]
    private class SaveRows
    {
        public SaveRow[] rows;
    }

    [Serializable]
    private class SaveSyncPayload
    {
        public SaveSlots.CloudSnapshot p_save_data;
        public string[] p_base_slot_timestamps;
    }
    [Serializable] private class SaveBase { public string[] slots; }

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
    [Serializable] private class QuestionAttemptRows { public QuestionAttempt[] rows; }

    public static CloudSaveSync Instance { get; private set; }

    private string projectUrl;
    private string publishableKey;
    private string userId;
    private bool sessionReady;
    private bool sessionActive;
    private bool initialSyncRunning;
    private bool pendingProgress;
    private bool applyingRemote;
    private int saveGeneration;
    private int inventoryGeneration;
    private SaveSlots.CloudSnapshot pendingGuestBootstrap;
    private string[] knownCloudTimes = new string[SaveSlots.Count];
    private bool saveConflictDetected;
    public string Status { get; private set; } = "";
    private string SuccessfulProgressStatus => saveConflictDetected
        ? "Newer account progress was kept. A copy of the conflicting device save is stored locally."
        : "Device and account progress synchronized.";
    private string SaveBaseKey => "CodeEscapade.CloudBase.v1." + UnityWebRequest.EscapeURL(projectUrl) + "." + userId;
    private bool requestRunning;
    private bool uploadQueued;
    private bool inventorySyncRunning;
    private bool inventoryChangeSyncQueued;
    private bool inventorySyncNeeded;
    private bool questionAttemptSyncRunning;
    private bool questionAttemptSyncQueued;
    private bool questionHistorySyncNeeded;
    private Coroutine questionAttemptRetryCoroutine;
    private Coroutine inventoryRetryCoroutine;

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
        QuestionPerformanceTracker.PendingAttemptsChanged += OnQuestionAttemptsChanged;
    }

    void OnDestroy()
    {
        SaveSlots.LocalSaveChanged -= OnLocalSaveChanged;
        CharacterInventory.InventoryChanged -= OnInventoryChanged;
        QuestionPerformanceTracker.PendingAttemptsChanged -= OnQuestionAttemptsChanged;
        if (questionAttemptRetryCoroutine != null) StopCoroutine(questionAttemptRetryCoroutine);
        if (inventoryRetryCoroutine != null) StopCoroutine(inventoryRetryCoroutine);
        if (Instance == this) Instance = null;
    }

    public void BeginSession(string url, string key, string token, string accountId)
    {
        EndSession();
        sessionActive = true;
        projectUrl = url.Trim().TrimEnd('/');
        publishableKey = key.Trim();
        userId = accountId;
        try
        {
            var savedBase = JsonUtility.FromJson<SaveBase>(PlayerPrefs.GetString(SaveBaseKey, "{}"));
            if (savedBase?.slots?.Length == SaveSlots.Count) knownCloudTimes = savedBase.slots;
        }
        catch (ArgumentException) { }
        sessionReady = false;
        questionHistorySyncNeeded = true;
        inventorySyncNeeded = true;
        StartCoroutine(RecoverSession());
    }

    public void EndSession()
    {
        StopAllCoroutines();
        SupabaseSession.CancelPendingRefresh();
        sessionActive = initialSyncRunning = pendingProgress = applyingRemote = false;
        saveGeneration = inventoryGeneration = 0;
        pendingGuestBootstrap = null;
        knownCloudTimes = new string[SaveSlots.Count];
        saveConflictDetected = false;
        Status = "";
        questionAttemptRetryCoroutine = null;
        inventoryRetryCoroutine = null;
        questionAttemptSyncQueued = false;
        questionAttemptSyncRunning = false;
        inventoryChangeSyncQueued = false;
        inventorySyncRunning = false;
        uploadQueued = false;
        requestRunning = false;
        sessionReady = false;
        projectUrl = publishableKey = userId = "";
        questionHistorySyncNeeded = false;
        inventorySyncNeeded = false;
        if (questionAttemptRetryCoroutine != null)
        {
            StopCoroutine(questionAttemptRetryCoroutine);
            questionAttemptRetryCoroutine = null;
        }
        if (inventoryRetryCoroutine != null)
        {
            StopCoroutine(inventoryRetryCoroutine);
            inventoryRetryCoroutine = null;
        }
        QuestionPerformanceTracker.SetSyncStatus("Attempts are saved on this device.");
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) RetryPendingInventoryNow();
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused) RetryPendingInventoryNow();
    }

    public void StartInitialSync(
        bool hasLocalProgress,
        SaveSlots.CloudSnapshot guestBootstrap,
        Action<string> completed)
    {
        pendingGuestBootstrap = guestBootstrap;
        StartCoroutine(InitialSyncRoutine(hasLocalProgress, guestBootstrap, completed));
    }

    IEnumerator InitialSyncRoutine(
        bool hasLocalProgress,
        SaveSlots.CloudSnapshot guestBootstrap,
        Action<string> completed)
    {
        if (initialSyncRunning || !sessionActive) yield break;
        initialSyncRunning = true;
        try
        {
            using (var request = CreateRequest(
                projectUrl + "/rest/v1/player_saves?user_id=eq." + userId + "&select=save_data", "GET"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Status = "Cloud sync is unavailable. Your progress is saved locally and will retry.";
                    Debug.LogWarning("Could not check cloud saves: " + GetError(request));
                    completed?.Invoke(Status);
                    yield break;
                }
                SaveRows rows = null;
                try { rows = JsonUtility.FromJson<SaveRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
                catch (ArgumentException) { }
                // Never treat a malformed response as an empty account and overwrite it.
                if (rows?.rows == null || (rows.rows.Length > 0 && rows.rows[0]?.save_data == null))
                {
                    Status = "Cloud save could not be read. Local progress is safe; syncing will retry.";
                    completed?.Invoke(Status);
                    yield break;
                }
                var cloud = rows.rows.Length == 0 ? null : rows.rows[0].save_data;
                var local = SaveSlots.CaptureCloudSnapshot();
                if (cloud != null && !SaveSlots.HasLocalProgress() && !pendingProgress &&
                    Array.TrueForAll(local.slotModifiedUtc, string.IsNullOrWhiteSpace))
                {
                    // A fresh account cache must restore the account's selected
                    // hero/slot, rather than treating guest-menu defaults as newer.
                    local.activeSlot = cloud.activeSlot;
                    local.selectedCharacter = cloud.selectedCharacter;
                    local.modifiedUtc = cloud.modifiedUtc;
                    ApplySnapshotIfChanged(local);
                }
                var merged = cloud != null ? local
                    : SaveSlots.MergeCloudSnapshots(local, guestBootstrap);
                // For an existing account, let the locked RPC resolve the versions
                // before replacing any device data (it can report a conflict).
                if (cloud == null && merged != null) ApplySnapshotIfChanged(merged);
                if (cloud != null || SaveSlots.HasProgress(merged) || pendingProgress)
                {
                    string error = null;
                    yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), value => error = value);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Status = "Progress is saved locally. Cloud sync will retry. " + error;
                        completed?.Invoke(Status);
                        yield break;
                    }
                }
                sessionReady = true;
                pendingGuestBootstrap = null;
                SaveSlots.MarkActiveProfileBootstrapComplete();
                Status = SuccessfulProgressStatus;
                yield return FinishInitialSync(Status, completed);
            }
        }
        finally { initialSyncRunning = false; }
    }

    void ApplySnapshotIfChanged(SaveSlots.CloudSnapshot snapshot)
    {
        if (JsonUtility.ToJson(SaveSlots.CaptureCloudSnapshot()) == JsonUtility.ToJson(snapshot)) return;
        applyingRemote = true;
        try { SaveSlots.ApplyCloudSnapshot(snapshot); }
        finally { applyingRemote = false; }
    }

    IEnumerator RecoverSession()
    {
        while (sessionActive)
        {
            yield return new WaitForSecondsRealtime(15f);
            if (!sessionActive) yield break;
            if (!sessionReady)
            {
                if (!initialSyncRunning)
                    yield return InitialSyncRoutine(SaveSlots.HasLocalProgress(), pendingGuestBootstrap, null);
                continue;
            }
            bool tokenReady = false;
            yield return SupabaseSession.EnsureAccessToken(value => tokenReady = value);
            if (!tokenReady) { Status = SupabaseSession.LastError; continue; }
            if (pendingProgress && !requestRunning && !inventorySyncRunning)
                yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), error =>
                {
                    Status = string.IsNullOrEmpty(error) ? SuccessfulProgressStatus
                        : "Progress is saved locally. Cloud sync will retry. " + error;
                });
            RetryPendingInventoryNow();
            if (questionHistorySyncNeeded && !questionAttemptSyncRunning && !questionAttemptSyncQueued)
                OnQuestionAttemptsChanged();
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
        if (sessionReady) yield return SyncQuestionAttemptsRoutine();
        Status = string.IsNullOrEmpty(status) ? inventoryWarning :
            status + (string.IsNullOrEmpty(inventoryWarning) ? "" : "\n" + inventoryWarning);
        completed?.Invoke(Status);
    }

    void OnQuestionAttemptsChanged()
    {
        questionHistorySyncNeeded = true;
        if (!sessionReady || questionAttemptSyncQueued) return;
        questionAttemptSyncQueued = true;
        StartCoroutine(SyncQuestionAttemptsAfterDelay());
    }

    IEnumerator SyncQuestionAttemptsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        questionAttemptSyncQueued = false;
        if (!sessionReady) yield break;
        yield return SyncQuestionAttemptsRoutine();
    }

    IEnumerator SyncQuestionAttemptsRoutine()
    {
        if (!sessionReady || string.IsNullOrWhiteSpace(userId)) yield break;
        while (questionAttemptSyncRunning) yield return null;
        questionAttemptSyncRunning = true;

        List<QuestionAttempt> pending = QuestionPerformanceTracker.PreparePendingForUser(userId);
        if (pending.Count > 0)
        {
            string url = projectUrl + "/rest/v1/player_question_attempts?on_conflict=attempt_id";
            using (var request = CreateRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(ToJsonArray(pending)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    QuestionPerformanceTracker.SetSyncStatus("Offline or sync failed. Attempts remain saved on this device. Run question_performance_schema.sql if the table is missing. " + GetError(request));
                    questionAttemptSyncRunning = false;
                    ScheduleQuestionAttemptRetry();
                    yield break;
                }
            }
            QuestionPerformanceTracker.MarkSynced(userId, pending.Select(attempt => attempt.attempt_id));
        }

        const int pageSize = 500;
        var downloaded = new System.Collections.Generic.List<QuestionAttempt>();
        for (int offset = 0; ; offset += pageSize)
        {
            string query = "?select=attempt_id,user_id,slot_index,question_id,topic,language,difficulty,source,player_level,outcome,presented_at,answered_at,duration_seconds&order=presented_at.asc&limit=" + pageSize;
            using (var request = CreateRequest(projectUrl + "/rest/v1/player_question_attempts" + query, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Range-Unit", "items");
                request.SetRequestHeader("Range", offset + "-" + (offset + pageSize - 1));
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    QuestionPerformanceTracker.SetSyncStatus("Attempts are saved locally; cloud history could not be loaded. Run question_performance_schema.sql if the table is missing. " + GetError(request));
                    questionAttemptSyncRunning = false;
                    ScheduleQuestionAttemptRetry();
                    yield break;
                }
                try
                {
                    QuestionAttemptRows page = JsonUtility.FromJson<QuestionAttemptRows>("{\"rows\":" + request.downloadHandler.text + "}");
                    if (page?.rows == null || page.rows.Length == 0) break;
                    downloaded.AddRange(page.rows);
                    if (page.rows.Length < pageSize) break;
                }
                catch (ArgumentException exception)
                {
                    QuestionPerformanceTracker.SetSyncStatus("Cloud history response could not be read: " + exception.Message);
                    questionAttemptSyncRunning = false;
                    ScheduleQuestionAttemptRetry();
                    yield break;
                }
            }
        }

        QuestionPerformanceTracker.MergeRemote(userId, downloaded);
        questionAttemptSyncRunning = false;
        questionHistorySyncNeeded = false;
    }

    void ScheduleQuestionAttemptRetry()
    {
        if (sessionReady && questionAttemptRetryCoroutine == null)
            questionAttemptRetryCoroutine = StartCoroutine(RetryQuestionAttempts());
    }

    IEnumerator RetryQuestionAttempts()
    {
        while (sessionReady && questionHistorySyncNeeded)
        {
            yield return new WaitForSecondsRealtime(60f);
            if (sessionReady && questionHistorySyncNeeded)
                yield return SyncQuestionAttemptsRoutine();
        }
        questionAttemptRetryCoroutine = null;
    }

    static string ToJsonArray(System.Collections.Generic.IEnumerable<QuestionAttempt> values)
    {
        return "[" + string.Join(",", values.Select(JsonUtility.ToJson)) + "]";
    }

    void OnLocalSaveChanged()
    {
        if (applyingRemote || !sessionActive) return;
        saveGeneration++;
        pendingProgress = true;
        if (!sessionReady || uploadQueued) return;
        uploadQueued = true;
        StartCoroutine(UploadAfterDelay());
    }

    void OnInventoryChanged()
    {
        if (applyingRemote || !sessionActive) return;
        inventoryGeneration++;
        inventorySyncNeeded = true;
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

    void RetryPendingInventoryNow()
    {
        if (!sessionReady || !inventorySyncNeeded || inventoryChangeSyncQueued) return;
        inventoryChangeSyncQueued = true;
        StartCoroutine(SyncInventoryAfterLocalChange());
    }

    void ScheduleInventoryRetry()
    {
        if (sessionReady && inventorySyncNeeded && inventoryRetryCoroutine == null)
            inventoryRetryCoroutine = StartCoroutine(RetryInventorySync());
    }

    IEnumerator RetryInventorySync()
    {
        while (sessionReady && inventorySyncNeeded)
        {
            yield return new WaitForSecondsRealtime(30f);
            if (sessionReady && inventorySyncNeeded)
                yield return SyncCardInventoryRoutine(message =>
                {
                    if (!string.IsNullOrEmpty(message)) Debug.LogWarning("Cloud card inventory retry failed: " + message);
                });
        }
        inventoryRetryCoroutine = null;
    }

    void InventorySyncFailed(string message, Action<string> completed)
    {
        inventorySyncRunning = false;
        inventorySyncNeeded = true;
        completed?.Invoke(message);
        ScheduleInventoryRetry();
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
            {
                Status = "Progress is saved locally. Cloud sync will retry. " + message;
                Debug.LogWarning("Cloud save sync failed: " + message);
            }
            else Status = SuccessfulProgressStatus;
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
        int inventoryVersion = inventoryGeneration;
        if (pendingProgress)
        {
            string error = null;
            yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), value => error = value);
            if (!string.IsNullOrEmpty(error))
            {
                InventorySyncFailed("Cards remain saved locally. " + error, completed);
                yield break;
            }
        }
        string statesUrl = projectUrl + "/rest/v1/player_card_inventory_slots?user_id=eq." +
                           UnityWebRequest.EscapeURL(userId) + "&select=slot_index,updated_at";
        InventoryStateRows stateRows = null;
        using (var request = CreateRequest(statesUrl, UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return SupabaseSession.Send(request);
            if (request.result != UnityWebRequest.Result.Success)
            {
                InventorySyncFailed("Card inventory table is not ready yet. Run card_inventory_schema.sql in Supabase. " + GetError(request), completed);
                yield break;
            }
            try { stateRows = JsonUtility.FromJson<InventoryStateRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
            catch (ArgumentException) { }
        }
        if (stateRows?.rows == null)
        {
            InventorySyncFailed("Cloud inventory timestamps could not be read; no inventory was changed.", completed);
            yield break;
        }

        InventoryCardRows cardRows = null;
        using (var request = CreateRequest(projectUrl + "/rest/v1/player_card_inventory?user_id=eq." +
                   UnityWebRequest.EscapeURL(userId) + "&select=slot_index,card_key:game_card_key,quantity,level", UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return SupabaseSession.Send(request);
            if (request.result != UnityWebRequest.Result.Success)
            {
                InventorySyncFailed("Could not read owned card rows: " + GetError(request), completed);
                yield break;
            }
            try { cardRows = JsonUtility.FromJson<InventoryCardRows>("{\"rows\":" + request.downloadHandler.text + "}"); }
            catch (ArgumentException) { }
        }
        if (cardRows?.rows == null)
        {
            InventorySyncFailed("Cloud inventory could not be read; local cards remain unchanged.", completed);
            yield break;
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

        if (appliedRemote)
        {
            string saveError = null;
            yield return UploadRoutine(SaveSlots.CaptureCloudSnapshot(), message => saveError = message);
            if (!string.IsNullOrEmpty(saveError))
            {
                InventorySyncFailed("Owned cards downloaded, but the combined save snapshot could not be updated: " + saveError, completed);
                yield break;
            }
        }

        for (int slot = 0; slot < SaveSlots.Count; slot++)
        {
            var localCards = CharacterInventory.ReadSavedCards(slot);
            bool hasCloudSlot = stateRows?.rows != null &&
                Array.Exists(stateRows.rows, row => row != null && row.slot_index == slot);
            // A never-synced empty slot has nothing to upload. Existing cloud
            // slots must still receive empty lists so removed cards stay removed.
            if (localCards.Count == 0 && !hasCloudSlot) continue;
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
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.downloadHandler.text.Contains("40001"))
                    {
                        // The full account snapshot changed since this inventory read.
                        // Reconcile it before attempting another inventory replacement.
                        sessionReady = false;
                        pendingProgress = true;
                    }
                    InventorySyncFailed("Could not sync owned cards for save slot " + (slot + 1) + ": " + GetError(request), completed);
                    yield break;
                }
            }
        }

        inventorySyncRunning = false;
        inventorySyncNeeded = inventoryVersion != inventoryGeneration;
        if (inventorySyncNeeded) ScheduleInventoryRetry();
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
        while (requestRunning && sessionActive) yield return null;
        if (!sessionActive) { completed?.Invoke("Sign in again to sync."); yield break; }
        requestRunning = true;
        // Another queued upload may have reconciled a conflict while we waited.
        // Never send its obsolete captured branch with the newly observed versions.
        snapshot = SaveSlots.CaptureCloudSnapshot();
        int generation = saveGeneration;
        pendingProgress = true;
        try
        {
            // The RPC locks and merges the server row. Direct REST upserts are disabled
            // by asap_database_fixes.sql so older clients cannot overwrite newer saves.
            var payload = new SaveSyncPayload { p_save_data = snapshot, p_base_slot_timestamps = knownCloudTimes };
            using (var request = CreateRequest(projectUrl + "/rest/v1/rpc/sync_player_save", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed?.Invoke(GetError(request));
                    yield break;
                }
                SaveRow result = null;
                try { result = JsonUtility.FromJson<SaveRow>(request.downloadHandler.text); }
                catch (ArgumentException) { }
                if (result?.save_data?.slots?.Length != SaveSlots.Count ||
                    result.save_data.inventories?.Length != SaveSlots.Count ||
                    result.save_data.slotModifiedUtc?.Length != SaveSlots.Count)
                {
                    completed?.Invoke("The server returned an unreadable save. Local progress remains pending.");
                    yield break;
                }
                var current = SaveSlots.CaptureCloudSnapshot();
                var combined = SaveSlots.MergeCloudSnapshots(current, result.save_data);
                saveConflictDetected = result.conflicted_slots != null && result.conflicted_slots.Length > 0;
                if (saveConflictDetected)
                {
                    // Preserve both versions: keep the newer account branch active and
                    // retain the full device branch, including edits made during this request.
                    if (!SaveConflictBackup(current))
                    {
                        completed?.Invoke("A save conflict was found, but its backup could not be written. Local progress was left unchanged.");
                        yield break;
                    }
                    foreach (int slot in result.conflicted_slots)
                    {
                        if (slot < 0 || slot >= SaveSlots.Count) continue;
                        combined.slots[slot] = result.save_data.slots[slot];
                        combined.inventories[slot] = result.save_data.inventories[slot];
                        combined.slotModifiedUtc[slot] = result.save_data.slotModifiedUtc[slot];
                    }
                }
                knownCloudTimes = (string[])result.save_data.slotModifiedUtc.Clone();
                PlayerPrefs.SetString(SaveBaseKey, JsonUtility.ToJson(new SaveBase { slots = knownCloudTimes }));
                PlayerPrefs.Save();
                ApplySnapshotIfChanged(combined);
                pendingProgress = generation != saveGeneration;
                completed?.Invoke("");
            }
        }
        finally { requestRunning = false; }
    }

    bool SaveConflictBackup(SaveSlots.CloudSnapshot snapshot)
    {
        try
        {
            string folder = Path.Combine(Application.persistentDataPath, "CloudSaveConflicts");
            Directory.CreateDirectory(folder);
            string name = userId + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N") + ".json";
            File.WriteAllText(Path.Combine(folder, name), JsonUtility.ToJson(snapshot));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not keep a local cloud-conflict backup: " + exception.Message);
            return false;
        }
    }

    UnityWebRequest CreateRequest(string url, string method)
    {
        var request = new UnityWebRequest(url, method);
        request.timeout = 20;
        request.SetRequestHeader("apikey", publishableKey);
        request.SetRequestHeader("Authorization", "Bearer " + MainMenuSaveSlots.CurrentAccessToken);
        request.SetRequestHeader("Accept", "application/json");
        return request;
    }

    static string GetError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text ?? "" : "";
        if (request.responseCode == 0 && !string.IsNullOrEmpty(SupabaseSession.LastError)) return SupabaseSession.LastError;
        if (body.Contains("PGRST202") || body.Contains("Invalid save slot"))
            return "The database needs the ASAP fixes migration before cloud sync can finish.";
        if (body.Contains("40001")) return "Newer progress was found on another device. Syncing will retry safely.";
        return string.IsNullOrWhiteSpace(body) ? request.error : body;
    }
}
