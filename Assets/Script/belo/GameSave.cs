using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSave : MonoBehaviour
{
    public static GameSave Instance;
    private SaveSlots.Record pendingRestore;
    private float nextAutoSave;

    public static GameSave EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<GameSave>();
            if (Instance == null) Instance = new GameObject("Game Save Manager").AddComponent<GameSave>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SaveSlots.MigrateLegacy();
    }

    void Update()
    {
        if (Time.timeScale <= 0f || pendingRestore != null || Time.unscaledTime < nextAutoSave) return;
        nextAutoSave = Time.unscaledTime + 15f;
        SaveCurrentPlayer();
    }

    public void SaveCurrentPlayer()
    {
        var player = FindFirstObjectByType<playerMovement>();
        if (player != null) SaveGame(player.transform);
    }

    public void SaveGame(Transform player)
    {
        string scene = SceneManager.GetActiveScene().name;
        if (player == null || (scene != "Scene 2" && scene != "Scene3") || pendingRestore != null) return;
        SaveSlots.Write(new SaveSlots.Record { scene = scene, hasPosition = true,
            position = player.position, character = PlayerPrefs.GetInt("SelectedCharacter", 0),
            defeatedEncounters = SaveSlots.Read(SaveSlots.Active)?.defeatedEncounters ?? new System.Collections.Generic.List<string>() });
    }

    public void ContinueGame()
    {
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved == null || !Application.CanStreamedLevelBeLoaded(saved.scene))
        {
            Debug.LogWarning("The selected save slot is empty or its scene is unavailable.");
            return;
        }
        pendingRestore = saved;
        PlayerPrefs.SetInt("SelectedCharacter", saved.character);
        Time.timeScale = 1f;
        SceneManager.LoadScene(saved.scene);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingRestore != null && scene.name == pendingRestore.scene)
            StartCoroutine(RestorePlayer());
        nextAutoSave = Time.unscaledTime + 15f;
    }

    IEnumerator RestorePlayer()
    {
        // Let scene Start methods spawn the selected character before restoring.
        yield return null;
        var saved = pendingRestore;
        if (saved == null) yield break;
        if (saved.hasPosition)
        {
            playerMovement player = null;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (player == null && Time.realtimeSinceStartup < deadline)
            {
                player = FindFirstObjectByType<playerMovement>();
                if (player == null) yield return null;
            }
            if (player != null)
            {
                player.transform.position = saved.position;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = saved.position; body.linearVelocity = Vector2.zero; }
                Physics2D.SyncTransforms();
            }
            else Debug.LogError("Could not find a player to restore the save position.");
        }
        pendingRestore = null;
    }

    public void NewGame()
    {
        pendingRestore = null;
        SaveSlots.BeginNew(PlayerPrefs.GetInt("SelectedCharacter", 0));
    }

    void OnApplicationPause(bool paused) { if (paused) SaveCurrentPlayer(); }
    void OnApplicationQuit() { SaveCurrentPlayer(); }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
