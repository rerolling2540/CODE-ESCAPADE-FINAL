using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSave : MonoBehaviour
{
    public static GameSave Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame(Transform player)
    {
        if (player == null)
        {
            Debug.LogError("PLAYER IS NOT ASSIGNED!");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetString("SavedScene", sceneName);

        PlayerPrefs.SetFloat("PlayerX", player.position.x);
        PlayerPrefs.SetFloat("PlayerY", player.position.y);
        PlayerPrefs.SetFloat("PlayerZ", player.position.z);

        PlayerPrefs.SetInt("HasGame", 1);

        PlayerPrefs.Save();

        Debug.Log("========== GAME SAVED ==========");
        Debug.Log("Scene: " + sceneName);
        Debug.Log("Player Position: " + player.position);
        Debug.Log("================================");
    }

    public void ContinueGame()
    {
        if (PlayerPrefs.GetInt("HasGame", 0) == 1)
        {
            string savedScene = PlayerPrefs.GetString(
                "SavedScene",
                "GameScene"
            );

            Debug.Log("========== CONTINUE GAME ==========");
            Debug.Log("Loading Scene: " + savedScene);

            SceneManager.sceneLoaded += OnSceneLoaded;

            SceneManager.LoadScene(savedScene);
        }
        else
        {
            Debug.LogWarning("NO SAVED GAME FOUND!");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        StartCoroutine(RestorePlayer());
    }

    IEnumerator RestorePlayer()
    {
        // Wait for player to spawn
        yield return new WaitForSeconds(1f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError(
                "PLAYER NOT FOUND! Make sure the character Tag is Player."
            );

            yield break;
        }

        float x = PlayerPrefs.GetFloat("PlayerX", 0f);
        float y = PlayerPrefs.GetFloat("PlayerY", 0f);
        float z = PlayerPrefs.GetFloat("PlayerZ", 0f);

        Vector3 savedPosition = new Vector3(x, y, z);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = new Vector2(x, y);
        }

        player.transform.position = savedPosition;

        Debug.Log("========== PLAYER RESTORED ==========");
        Debug.Log("Restored Position: " + savedPosition);
        Debug.Log("=====================================");
    }

    public void NewGame()
    {
        PlayerPrefs.DeleteKey("SavedScene");
        PlayerPrefs.DeleteKey("PlayerX");
        PlayerPrefs.DeleteKey("PlayerY");
        PlayerPrefs.DeleteKey("PlayerZ");
        PlayerPrefs.DeleteKey("HasGame");

        PlayerPrefs.Save();

        Debug.Log("NEW GAME SAVE DELETED!");
    }
}