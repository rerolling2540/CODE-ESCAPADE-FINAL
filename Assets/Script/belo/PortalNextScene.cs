
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PortalNextScene : MonoBehaviour
{
    public string nextSceneName = "NextScene";
    public AudioClip portalSound;

    [Range(0f, 5f)]
    public float soundVolume = 3f;

    [Header("Portal Delay")]
    public float portalDelay = 2f;

    private bool isEntering = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isEntering || !other.CompareTag("Player"))
            return;

        isEntering = true;
        StartCoroutine(EnterPortal());
    }

    IEnumerator EnterPortal()
    {
        if (portalSound != null)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = portalSound;
            source.volume = soundVolume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();

            // Hintayin matapos ang sound
            yield return new WaitForSecondsRealtime(
                portalSound.length
            );

            Destroy(source);
        }

        // Dagdag na delay bago lumipat ng Scene
        yield return new WaitForSecondsRealtime(portalDelay);

        SceneManager.LoadScene(nextSceneName);
    }
}
