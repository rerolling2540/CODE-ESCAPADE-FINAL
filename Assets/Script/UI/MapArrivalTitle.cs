using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored, non-interactive map arrival title.</summary>
public sealed class MapArrivalTitle : MonoBehaviour
{
    private static string previousScene;
    private static string lastMap;
    private static int pendingSceneHandle = -1;
    private static MapArrivalTitle activeTitle;

    public static bool IsAnnouncing(Scene scene) => activeTitle != null &&
        activeTitle.IsCurrentMap && activeTitle.gameObject.scene == scene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        previousScene = lastMap = null;
        pendingSceneHandle = -1;
        activeTitle = null;
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneEvents()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        if (SceneManager.GetActiveScene().name == "Battle Scene") previousScene = "Battle Scene";
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        pendingSceneHandle = -1;
        if (activeTitle != null) activeTitle.Hide();
        if (CharacterAlmanac.IsExplorationScene(scene.name))
        {
            string map = MapSceneNames.Resolve(scene.name);
            if (ShouldAnnounce(previousScene, lastMap, map)) pendingSceneHandle = scene.handle;
            lastMap = map;
        }
        else if (scene.name == "MainMenu" || scene.name == "StoryScene") lastMap = null;
        previousScene = scene.name;
    }

    private static bool ShouldAnnounce(string previous, string last, string next)
    {
        next = MapSceneNames.Resolve(next);
        last = MapSceneNames.Resolve(last);
        if (!CharacterAlmanac.IsExplorationScene(next)) return false;
        bool battleReturn = previous == "Battle Scene" && (last == null || last == next);
        return last != next && !battleReturn;
    }

    [SerializeField] private CanvasGroup banner;
    [SerializeField] private RectTransform motionRoot;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text titleShadow;
    [SerializeField, Min(0.1f)] private float fadeInDuration = 0.55f;
    [SerializeField, Min(0.1f)] private float holdDuration = 2.6f;
    [SerializeField, Min(0.1f)] private float fadeOutDuration = 0.7f;

    private void Awake()
    {
        if (banner == null) return;
        banner.alpha = 0f;
        banner.interactable = false;
        banner.blocksRaycasts = false;
        banner.ignoreParentGroups = false;
        string mapName = gameObject.scene.name.ToUpperInvariant();
        if (title != null) title.text = mapName;
        if (titleShadow != null) titleShadow.text = mapName;
    }

    private bool IsCurrentMap => CharacterAlmanac.IsExplorationScene(gameObject.scene.name) &&
        gameObject.scene == SceneManager.GetActiveScene();

    private bool Hidden => !IsCurrentMap || SceneTransitionFader.IsTransitioning ||
        GameControlsGuide.GameplayOverlayHidden || MonolithTravelPanel.IsOpen ||
        Time.timeScale <= 0f ||
        (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive);

    private IEnumerator Start()
    {
        if (banner == null || motionRoot == null) yield break;
        if (!IsCurrentMap) yield break;
        // Editor Play mode may keep the existing scene instead of loading it again.
        if (previousScene == null) SceneLoaded(gameObject.scene, LoadSceneMode.Single);
        if (pendingSceneHandle != gameObject.scene.handle) yield break;
        pendingSceneHandle = -1;
        activeTitle = this;
        // Let arrival positioning and the portal's black overlay finish first.
        yield return null;
        while (Hidden)
        {
            if (!IsCurrentMap) { Hide(); yield break; }
            yield return null;
        }

        Vector2 restingPosition = motionRoot.anchoredPosition;
        float elapsed = 0f;
        float duration = fadeInDuration + holdDuration + fadeOutDuration;
        while (elapsed < duration)
        {
            if (Hidden)
            {
                Hide();
                motionRoot.anchoredPosition = restingPosition;
                yield break;
            }

            float entering = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInDuration));
            float leaving = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((elapsed - fadeInDuration - holdDuration) / fadeOutDuration));
            banner.alpha = entering * (1f - leaving);
            motionRoot.anchoredPosition = restingPosition + Vector2.up * (-12f * (1f - entering) + 8f * leaving);
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            yield return null;
        }
        Hide();
        motionRoot.anchoredPosition = restingPosition;
    }

    private void Hide()
    {
        if (banner != null) banner.alpha = 0f;
        if (activeTitle == this) activeTitle = null;
    }

    private void LateUpdate() { if (!IsCurrentMap) Hide(); }
    private void OnDisable() => Hide();
}
