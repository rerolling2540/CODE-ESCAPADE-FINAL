using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent battle BGM player, kept separate from BattleAudio's sound effects.</summary>
public sealed class BattleBgmController : MonoBehaviour
{
    private enum MusicState { Stopped, Transition, Battle, Victory, Defeat }

    private const float TransitionDuration = 5f;
    private const string LibraryResourcePath = "BattleMusicLibrary";
    private static BattleBgmController instance;

    private AudioSource source;
    private BattleMusicLibrary library;
    private Coroutine scheduledBattleMusic;
    private Coroutine timedStop;
    private MusicState state;
    private float transitionEndsAt;

    public static void PlayBattleTransition()
    {
        var player = EnsureInstance();
        if (player == null || player.library == null || player.library.transitionAndVictory == null) return;
        player.PlayTimed(player.library.transitionAndVictory, TransitionDuration, MusicState.Transition);
        player.transitionEndsAt = Time.unscaledTime + TransitionDuration;
    }

    public static void EnterBattle()
    {
        var player = EnsureInstance();
        if (player == null || player.library == null) return;
        if (player.state == MusicState.Transition && player.source.isPlaying)
        {
            if (player.scheduledBattleMusic != null) player.StopCoroutine(player.scheduledBattleMusic);
            player.scheduledBattleMusic = player.StartCoroutine(player.StartBattleAfterTransition());
            return;
        }
        player.StartBattlePlaylist();
    }

    public static void PlayVictory()
    {
        var player = EnsureInstance();
        if (player == null || player.library == null || player.library.transitionAndVictory == null) return;
        player.PlayTimed(player.library.transitionAndVictory, TransitionDuration, MusicState.Victory);
    }

    public static void PlayDefeat()
    {
        var player = EnsureInstance();
        if (player == null || player.library == null || player.library.defeat == null) return;
        player.StopScheduledMusic();
        player.source.Stop();
        player.source.clip = player.library.defeat;
        player.source.loop = false;
        player.source.Play();
        player.state = MusicState.Defeat;
    }

    private static BattleBgmController EnsureInstance()
    {
        if (instance != null) return instance;
        var gameObject = new GameObject("Battle Background Music");
        return gameObject.AddComponent<BattleBgmController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        library = Resources.Load<BattleMusicLibrary>(LibraryResourcePath);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        GameAudio.Route(source, true);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (library == null)
            Debug.LogError($"Battle music library was not found at Resources/{LibraryResourcePath}.");
    }

    private IEnumerator StartBattleAfterTransition()
    {
        float remaining = Mathf.Max(0f, transitionEndsAt - Time.unscaledTime);
        if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);
        scheduledBattleMusic = null;
        if (state == MusicState.Transition) StartBattlePlaylist();
    }

    private void StartBattlePlaylist()
    {
        if (library == null) return;
        StopScheduledMusic();
        source.Stop();
        state = MusicState.Battle;

        if (library.battleIntro != null)
            StartCoroutine(PlayIntroThenLoop(library.battleIntro, library.battleLoop));
        else if (library.battleLoop != null)
            PlayLoop(library.battleLoop);
    }

    private IEnumerator PlayIntroThenLoop(AudioClip intro, AudioClip loop)
    {
        source.clip = intro;
        source.loop = false;
        source.Play();
        yield return new WaitWhile(() => source != null && source.isPlaying && state == MusicState.Battle);
        if (state == MusicState.Battle && loop != null) PlayLoop(loop);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null) return;
        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    private void PlayTimed(AudioClip clip, float seconds, MusicState newState)
    {
        StopScheduledMusic();
        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.Play();
        state = newState;
        timedStop = StartCoroutine(StopAfter(seconds, newState));
    }

    private IEnumerator StopAfter(float seconds, MusicState expectedState)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (state == expectedState)
        {
            source.Stop();
            // The battle-start coroutine takes over at the same five-second boundary.
            // Keep Transition state until it can switch to the loop; otherwise clear
            // the short transition/victory cue's state once its playback is complete.
            if (expectedState != MusicState.Transition || scheduledBattleMusic == null)
                state = MusicState.Stopped;
        }
        timedStop = null;
    }

    private void StopScheduledMusic()
    {
        if (scheduledBattleMusic != null)
        {
            StopCoroutine(scheduledBattleMusic);
            scheduledBattleMusic = null;
        }
        if (timedStop != null)
        {
            StopCoroutine(timedStop);
            timedStop = null;
        }
        StopAllCoroutines();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Battle Scene" || state == MusicState.Victory) return;
        if (state == MusicState.Battle || state == MusicState.Defeat || state == MusicState.Transition)
        {
            StopScheduledMusic();
            if (source != null) source.Stop();
            state = MusicState.Stopped;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
