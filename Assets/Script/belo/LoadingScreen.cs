using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public TMP_Text gameTitle;
    public TMP_Text subtitleText;
    public TMP_Text loadingText;
    public Slider loadingBar;

    public string nextScene = "GameScene";

    void Start()
    {
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        // Start both texts invisible
        Color titleColor = gameTitle.color;
        Color subtitleColor = subtitleText.color;

        titleColor.a = 0;
        subtitleColor.a = 0;

        gameTitle.color = titleColor;
        subtitleText.color = subtitleColor;

        // Fade in both at the same time
        float duration = 5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Clamp01(timer / duration);

            titleColor.a = alpha;
            subtitleColor.a = alpha;

            gameTitle.color = titleColor;
            subtitleText.color = subtitleColor;

            yield return null;
        }

        // Reset loading bar
        loadingBar.value = 0f;
        loadingText.text = "Loading... 0%";

        // 0% → 25%
        yield return StartCoroutine(FillBar(0f, 0.25f, 1.5f));

        // 25% → 50%
        yield return StartCoroutine(FillBar(0.25f, 0.50f, 1.5f));

        // 50% → 75%
        yield return StartCoroutine(FillBar(0.50f, 0.75f, 1.5f));

        // 75% → 100%
        yield return StartCoroutine(FillBar(0.75f, 1f, 1.5f));

        // Load game scene
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator FillBar(float start, float end, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Lerp(
                start,
                end,
                timer / duration
            );

            loadingBar.value = progress;

            int percentage = Mathf.RoundToInt(progress * 100);
            loadingText.text = "Loading... " + percentage + "%";

            yield return null;
        }

        loadingBar.value = end;

        int finalPercentage = Mathf.RoundToInt(end * 100);
        loadingText.text = "Loading... " + finalPercentage + "%";
    }
}