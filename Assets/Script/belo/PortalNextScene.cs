
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalNextScene : MonoBehaviour
{
    public string nextSceneName = "NextScene";
    public AudioClip portalSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private bool isEntering = false;

    private void Start()
    {
        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isEntering && !string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName),
            player => { isEntering = true; SceneManager.LoadScene(nextSceneName); });
    }
}
