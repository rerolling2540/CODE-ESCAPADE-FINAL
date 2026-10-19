
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private static AudioClip defaultSound;
    private bool isEntering = false;

    private void Awake()
    {
        var audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        var interaction = PortalInteraction.For(gameObject);
        interaction.AddRoute(
            () => isActiveAndEnabled && changeScene && !isEntering && !string.IsNullOrEmpty(destinationScene) && Application.CanStreamedLevelBeLoaded(destinationScene),
            player => { isEntering = true; SceneManager.LoadScene(destinationScene); });
    }

    public static void Preload()
    {
        if (defaultSound == null) defaultSound = Resources.Load<AudioClip>("PortalAudio/PortalEnter");
        if (defaultSound != null && defaultSound.loadState == AudioDataLoadState.Unloaded)
            defaultSound.LoadAudioData();
    }

    public static void PlayFor(GameObject portal)
    {
        Preload();
        AudioClip clip = null;
        float gain = 1f;
        // Select a valid clip even when an empty duplicate component comes first.
        foreach (var sound in portal.GetComponents<PortalEntrySound>())
            if (sound.isActiveAndEnabled && sound.portalSound != null)
            { clip = sound.portalSound; gain = Mathf.Clamp01(sound.volume); break; }
        var nextScene = portal.GetComponent<PortalNextScene>();
        if (clip == null && nextScene != null && nextScene.portalSound != null)
        { clip = nextScene.portalSound; gain = Mathf.Clamp01(nextScene.soundVolume); }
        if (clip == null) clip = defaultSound;
        if (clip == null) return;

        // A 2D source stays audible after teleporting away or loading another scene.
        var playback = new GameObject("Portal Entry Sound");
        DontDestroyOnLoad(playback);
        var source = playback.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 32;
        source.volume = gain;
        source.clip = clip;
        GameAudio.Route(source, false);
        source.Play();
        Destroy(playback, clip.length + 0.2f);
    }
}
