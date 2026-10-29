using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSave : MonoBehaviour
{
    public static GameSave Instance;
    private SaveSlots.Record pendingRestore;
    private float nextAutoSave;
    private bool freshTutorialStart;

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

    public bool CanSaveCurrentPlayer => pendingRestore == null && !PlayerDeathFlow.IsActive &&
        PlayerHealth.IsInitialized && PlayerHealth.CurrentHealth > 0 &&
        CharacterAlmanac.IsExplorationScene(SceneManager.GetActiveScene().name) &&
        FindFirstObjectByType<playerMovement>() != null;

    public bool ManualSave()
    {
        if (!CanSaveCurrentPlayer) return false;
        SaveCurrentPlayer();
        nextAutoSave = Time.unscaledTime + 15f;
        return true;
    }

    public void SaveGame(Transform player)
    {
        string scene = SceneManager.GetActiveScene().name;
        if (player == null || PlayerDeathFlow.IsActive || PlayerHealth.CurrentHealth <= 0 ||
            (scene != "Scene 2" && scene != "Scene3" && scene != "Scene4" && scene != "map 4") || pendingRestore != null) return;
        var saved = SaveSlots.Read(SaveSlots.Active) ?? new SaveSlots.Record();
        PlayerHealth.CopyTo(saved);
        saved.scene = scene;
        saved.hasPosition = true;
        saved.position = player.position;
        saved.character = PlayerPrefs.GetInt("SelectedCharacter", 0);
        SaveSlots.Write(saved);
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
        freshTutorialStart = false;
        PlayerPrefs.SetInt("SelectedCharacter", saved.character);
        Time.timeScale = 1f;
        SceneManager.LoadScene(saved.scene);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingRestore != null && scene.name == pendingRestore.scene)
            StartCoroutine(RestorePlayer());
        else if (freshTutorialStart && scene.name == "Scene 2")
            StartCoroutine(StartAtTutorial());
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
        else if (saved.scene == "Scene 2")
        {
            var player = FindFirstObjectByType<playerMovement>();
            if (player != null) PlaceAtTutorial(player);
        }
        pendingRestore = null;
    }

    IEnumerator StartAtTutorial()
    {
        yield return null;
        float deadline = Time.realtimeSinceStartup + 5f;
        playerMovement player = null;
        while (player == null && Time.realtimeSinceStartup < deadline)
        {
            player = FindFirstObjectByType<playerMovement>();
            if (player == null) yield return null;
        }
        if (player != null) PlaceAtTutorial(player);
        freshTutorialStart = false;
    }

    private static void PlaceAtTutorial(playerMovement player)
    {
        var start = GameObject.Find("Tutorial Start");
        if (start == null) return;
        Vector3 destination = start.transform.position;
        var collider = player.GetComponent<Collider2D>();
        float feetOffset = collider != null ? collider.bounds.min.y - player.transform.position.y : -0.8f;
        foreach (var hit in Physics2D.RaycastAll(destination, Vector2.down, 20f))
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.CompareTag("Ground") && hit.normal.y > 0.5f)
            { destination.y = hit.point.y - feetOffset + 0.05f; break; }
        player.transform.position = destination;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = destination; body.linearVelocity = Vector2.zero; }
        Physics2D.SyncTransforms();
    }

    public void NewGame()
    {
        pendingRestore = null;
        freshTutorialStart = true;
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
