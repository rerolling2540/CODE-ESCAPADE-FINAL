
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControlsTutorial : MonoBehaviour
{
    public GameObject controlsPanel;
    public float fadeDuration = 1.5f;

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = controlsPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = controlsPanel.AddComponent<CanvasGroup>();
        }

        controlsPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        Time.timeScale = 0f;

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
