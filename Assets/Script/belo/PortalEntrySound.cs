
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PortalEntrySound : MonoBehaviour
{
    [Header("Portal Sound")]
    public AudioClip portalSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Portal Settings")]
    public string playerTag = "Player";
    public bool changeScene = false;
    public string destinationScene;
    public float delayBeforeTeleport = 0.5f;

    private AudioSource audioSource;
    private bool isEntering = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isEntering || !other.CompareTag(playerTag))
            return;

        if (portalSound != null)
            audioSource.PlayOneShot(portalSound, volume);
        else
            Debug.LogWarning("Assign Portal Sound!", this);

        if (changeScene)
        {
            isEntering = true;
            StartCoroutine(ChangeSceneAfterSound());
        }
    }

    private IEnumerator ChangeSceneAfterSound()
    {
        float waitTime = portalSound != null
            ? Mathf.Max(delayBeforeTeleport, portalSound.length)
            : delayBeforeTeleport;

        yield return new WaitForSeconds(waitTime);

        if (!string.IsNullOrEmpty(destinationScene))
            SceneManager.LoadScene(destinationScene);
        else
            isEntering = false;
    }
}
