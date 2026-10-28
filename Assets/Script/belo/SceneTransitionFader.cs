using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Persistent full-screen fade used when moving between scenes from a portal.</summary>
public sealed class SceneTransitionFader : MonoBehaviour
{
    private static SceneTransitionFader instance;
    private Image overlay;
    private bool transitioning;
    public static bool IsTransitioning => instance != null && instance.transitioning;

    public static void Load(string sceneName)
    {
        if (instance == null)
            Create();

        if (!instance.transitioning)
            instance.StartCoroutine(instance.LoadWithFade(sceneName));
    }

    private static void Create()
    {
        var root = new GameObject("Scene Transition Fade");
        instance = root.AddComponent<SceneTransitionFader>();
        DontDestroyOnLoad(root);

        var canvasObject = new GameObject("Fade Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var imageObject = new GameObject("Black Overlay", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        instance.overlay = imageObject.GetComponent<Image>();
        instance.overlay.color = new Color(0f, 0f, 0f, 0f);
        instance.overlay.raycastTarget = false;
    }

    private IEnumerator LoadWithFade(string sceneName)
    {
        transitioning = true;
        yield return FadeTo(1f, 0.45f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load != null)
        {
            while (!load.isDone)
                yield return null;
        }

        yield return FadeTo(0f, 0.45f);
        transitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = overlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
