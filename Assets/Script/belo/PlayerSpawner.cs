
using UnityEngine;
using Unity.Cinemachine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject[] characterPrefabs;
    public Transform spawnPoint;
    public CinemachineCamera playerCamera;

    void Start()
    {
        if (characterPrefabs == null ||
            characterPrefabs.Length == 0 ||
            spawnPoint == null)
        {
            Debug.LogError("Missing Prefabs or Spawn Point!");
            return;
        }

        int selectedCharacter =
            PlayerPrefs.GetInt("SelectedCharacter", 0);

        selectedCharacter = Mathf.Clamp(
            selectedCharacter,
            0,
            characterPrefabs.Length - 1
        );

        GameObject player = Instantiate(
            characterPrefabs[selectedCharacter],
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log("Selected index: " + selectedCharacter);
        Debug.Log("Spawned player: " + player.name);

        if (playerCamera != null)
        {
            playerCamera.Follow = player.transform;
        }
        else
        {
            Debug.LogError("Assign the Cinemachine Camera!");
        }
    }
}
