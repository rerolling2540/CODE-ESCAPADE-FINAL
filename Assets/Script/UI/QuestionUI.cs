using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class QuestionUI : MonoBehaviour
{
    [Header("General Question UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text codeSnippetText;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private CardView playedCardPrefab;

    [Header("4 Choice Buttons (A-D)")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TMP_Text[] optionTexts = new TMP_Text[4];

    private Action<int> onOptionSelected;
    private Coroutine timerCoroutine;
    private Coroutine transitionCoroutine;
    private bool isAnswered;
    private CanvasGroup canvasGroup;
    private RectTransform scrollRect;
    private Image overlayImage;
    private CardView playedCardInstance;
    private RawImage playedCardImage;
    private Camera playedCardCamera;
    private RenderTexture playedCardTexture;
    private const int PlayedCardLayer = 31;
    private static Sprite scrollSprite;

    private CanvasGroup EnsureCanvasGroup()
    {
        // Reacquire the component in case it was removed/recreated while the panel was open.
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    public void SetupAndShow(QuestionData data, Action<int> callback, Action onTimeOut, Card playedCard = null)
    {
        if (data == null)
        {
            Debug.LogError("QuestionUI cannot show a null question.", this);
            onTimeOut?.Invoke();
            return;
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        isAnswered = false;
        onOptionSelected = callback;
        ApplyScrollTheme();
        ShowPlayedCard(playedCard);

        if (questionText == null)
        {
            Debug.LogError("QuestionUI has no question text assigned.", this);
            onTimeOut?.Invoke();
            gameObject.SetActive(false);
            return;
        }

        questionText.text = data.questionText;

        if (data is MultipleChoiceQuestionData mcData)
        {
            if (codeSnippetText != null)
            {
                codeSnippetText.gameObject.SetActive(!string.IsNullOrEmpty(mcData.codeSnippet));
                codeSnippetText.text = mcData.codeSnippet;
            }
            SetOptions(mcData.options);
        }
        else if (data is FillInBlankQuestionData fibData)
        {
            if (codeSnippetText != null)
            {
                codeSnippetText.gameObject.SetActive(true);
                codeSnippetText.text = fibData.codeWithBlank;
            }
            SetOptions(fibData.optionBlanks);
        }
        else
        {
            Debug.LogError($"Question type '{data.GetType().Name}' is not supported by QuestionUI.", this);
            onTimeOut?.Invoke();
            gameObject.SetActive(false);
            return;
        }

        LayoutQuestionForCurrentContent();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] == null) continue;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

        EnsureCanvasGroup();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        SetPanelOpenState(false);
        transitionCoroutine = StartCoroutine(OpenAndStartTimer(data.timeLimitInSeconds, onTimeOut));
    }

    private void SetOptions(string[] choices)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool hasChoice = choices != null && i < choices.Length && !string.IsNullOrEmpty(choices[i]);
            if (optionButtons[i] != null) optionButtons[i].gameObject.SetActive(hasChoice);
            if (hasChoice && i < optionTexts.Length && optionTexts[i] != null)
                optionTexts[i].text = choices[i];
        }
    }

    private void ApplyScrollTheme()
    {
        EnsureCanvasGroup();
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // The authored panel was offset below the canvas, clipping its question and answers.
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        if (overlayImage == null) overlayImage = GetComponent<Image>();
        if (overlayImage != null)
        {
            overlayImage.color = new Color(0.035f, 0.025f, 0.018f, 0.22f);
            overlayImage.raycastTarget = true;
        }

        Transform background = transform.Find("Background");
        if (background != null)
        {
            scrollRect = background.GetComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = Vector2.zero;
            scrollRect.sizeDelta = new Vector2(1180f, 820f);
            var image = background.GetComponent<Image>();
            if (image != null)
            {
                if (scrollSprite == null) scrollSprite = CreateScrollSprite();
                image.sprite = scrollSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }

        if (questionText == null || codeSnippetText == null)
            Debug.LogError("QuestionUI is missing its question or code snippet text reference.", this);

        RectTransform questionRect = questionText != null ? questionText.rectTransform : null;
        if (questionRect != null)
        {
            if (scrollRect != null) questionRect.SetParent(scrollRect, false);
            questionRect.anchorMin = questionRect.anchorMax = new Vector2(0.5f, 0.5f);
            questionRect.pivot = new Vector2(0.5f, 0.5f);
            questionRect.anchoredPosition = new Vector2(0f, 185f);
            questionRect.sizeDelta = new Vector2(1060f, 100f);
        }

        RectTransform snippetRect = codeSnippetText != null ? codeSnippetText.rectTransform : null;
        if (snippetRect != null)
        {
            if (scrollRect != null) snippetRect.SetParent(scrollRect, false);
            snippetRect.anchorMin = snippetRect.anchorMax = new Vector2(0.5f, 0.5f);
            snippetRect.pivot = new Vector2(0.5f, 0.5f);
            snippetRect.anchoredPosition = new Vector2(0f, 25f);
            snippetRect.sizeDelta = new Vector2(1060f, 160f);
        }

        RectTransform answersRect = transform.Find("Answers") as RectTransform;
        if (answersRect != null)
        {
            if (scrollRect != null) answersRect.SetParent(scrollRect, false);
            answersRect.anchorMin = answersRect.anchorMax = new Vector2(0.5f, 0.5f);
            answersRect.anchoredPosition = new Vector2(0f, -130f);
            answersRect.sizeDelta = Vector2.zero;
        }
        Vector2[] answerPositions =
        {
            new Vector2(-250f, -115f), new Vector2(250f, -115f),
            new Vector2(-250f, -200f), new Vector2(250f, -200f)
        };
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            RectTransform optionRect = optionButtons[i].GetComponent<RectTransform>();
            if (optionRect == null) continue;
            optionRect.SetParent(scrollRect != null ? scrollRect : optionRect.parent, false);
            optionRect.anchorMin = optionRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionRect.pivot = new Vector2(0.5f, 0.5f);
            optionRect.sizeDelta = new Vector2(480f, 68f);
            optionRect.anchoredPosition = answerPositions[i % answerPositions.Length];
        }
        RectTransform timerRect = timerSlider != null ? timerSlider.GetComponent<RectTransform>() : null;
        if (timerRect != null)
        {
            if (scrollRect != null) timerRect.SetParent(scrollRect, false);
            timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 0.5f);
            timerRect.anchoredPosition = new Vector2(440f, 270f);
            timerRect.sizeDelta = new Vector2(270f, 30f);
        }

        if (questionText != null)
        {
            questionText.color = new Color(0.12f, 0.075f, 0.035f);
            questionText.fontStyle = FontStyles.Bold;
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.enableAutoSizing = true;
            questionText.fontSizeMin = 23f;
            questionText.fontSizeMax = 38f;
        }
        if (codeSnippetText != null)
        {
            codeSnippetText.color = new Color(0.10f, 0.075f, 0.05f);
            codeSnippetText.alignment = TextAlignmentOptions.Midline;
            codeSnippetText.enableAutoSizing = true;
            codeSnippetText.fontSizeMin = 19f;
            codeSnippetText.fontSizeMax = 29f;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            Image buttonImage = optionButtons[i].targetGraphic as Image;
            if (buttonImage != null) BattleUITheme.DecoratePanel(buttonImage);
            if (i < optionTexts.Length && optionTexts[i] != null)
            {
                optionTexts[i].color = new Color(0.97f, 0.86f, 0.65f);
                optionTexts[i].fontStyle = FontStyles.Bold;
                optionTexts[i].alignment = TextAlignmentOptions.Center;
            }
        }

        if (timerSlider != null)
        {
            timerSlider.interactable = false;
            var backgroundImage = timerSlider.GetComponent<Image>();
            if (backgroundImage != null) backgroundImage.color = new Color(0.2f, 0.1f, 0.045f);
            if (timerSlider.fillRect != null)
            {
                var fillImage = timerSlider.fillRect.GetComponent<Image>();
                if (fillImage != null) fillImage.color = new Color(0.78f, 0.49f, 0.16f);
            }
        }
    }

    private void LayoutQuestionForCurrentContent()
    {
        bool hasCode = codeSnippetText != null && codeSnippetText.gameObject.activeSelf;
        if (questionText != null)
            questionText.rectTransform.anchoredPosition = new Vector2(0f, hasCode ? 185f : 130f);

        if (codeSnippetText != null)
        {
            codeSnippetText.rectTransform.anchoredPosition = new Vector2(0f, hasCode ? 25f : 15f);
            codeSnippetText.rectTransform.sizeDelta = new Vector2(1060f, hasCode ? 160f : 0f);
        }

        Vector2 firstRow = hasCode ? new Vector2(-250f, -115f) : new Vector2(-250f, 20f);
        Vector2 secondRow = hasCode ? new Vector2(-250f, -200f) : new Vector2(-250f, -65f);
        Vector2[] positions =
        {
            firstRow, new Vector2(250f, firstRow.y),
            secondRow, new Vector2(250f, secondRow.y)
        };
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            optionButtons[i].GetComponent<RectTransform>().anchoredPosition = positions[i];
        }
    }

    private void ShowPlayedCard(Card card)
    {
        ClearPlayedCard();
        if (card == null) return;
        if (playedCardPrefab == null)
        {
            Debug.LogError("QuestionUI needs the CardView prefab assigned to show the played card.", this);
            return;
        }

        EnsurePlayedCardImage();
        playedCardInstance = Instantiate(playedCardPrefab, Vector3.zero, Quaternion.identity);
        playedCardInstance.Setup(card);
        playedCardInstance.enabled = false;
        foreach (Collider collider in playedCardInstance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Collider2D collider in playedCardInstance.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        foreach (Transform child in playedCardInstance.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = PlayedCardLayer;

        SpriteRenderer[] renderers = playedCardInstance.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length > 0 && playedCardCamera != null)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            playedCardCamera.orthographic = true;
            playedCardCamera.orthographicSize = Mathf.Max(0.1f, bounds.extents.y * 1.12f);
            playedCardCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
            playedCardCamera.transform.rotation = Quaternion.identity;
            playedCardCamera.enabled = true;
            playedCardCamera.Render();
            playedCardCamera.enabled = false;
        }
        foreach (Renderer renderer in playedCardInstance.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        playedCardImage.gameObject.SetActive(true);
        playedCardImage.transform.SetAsLastSibling();
    }

    private void ClearPlayedCard()
    {
        if (playedCardInstance != null) Destroy(playedCardInstance.gameObject);
        playedCardInstance = null;
        if (playedCardCamera != null) playedCardCamera.enabled = false;
        if (playedCardImage != null) playedCardImage.gameObject.SetActive(false);
    }

    private void EnsurePlayedCardImage()
    {
        if (playedCardImage == null)
        {
            var imageObject = new GameObject("Played Card Prefab Preview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-665f, 90f);
            rect.sizeDelta = new Vector2(340f, 490f);
            playedCardImage = imageObject.GetComponent<RawImage>();
            playedCardImage.raycastTarget = false;
            imageObject.SetActive(false);
        }

        if (playedCardCamera == null)
        {
            var cameraObject = new GameObject("Played Card Preview Camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideInHierarchy;
            playedCardCamera = cameraObject.GetComponent<Camera>();
            playedCardCamera.clearFlags = CameraClearFlags.SolidColor;
            playedCardCamera.backgroundColor = Color.clear;
            playedCardCamera.cullingMask = 1 << PlayedCardLayer;
            playedCardCamera.orthographic = true;
            playedCardCamera.nearClipPlane = 0.1f;
            playedCardCamera.farClipPlane = 100f;
            playedCardCamera.allowHDR = false;
            playedCardCamera.allowMSAA = false;
            playedCardCamera.enabled = false;

            playedCardTexture = new RenderTexture(512, 736, 16, RenderTextureFormat.ARGB32)
            {
                name = "Played Card Preview Texture",
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
            playedCardTexture.Create();
            playedCardCamera.targetTexture = playedCardTexture;
            playedCardImage.texture = playedCardTexture;
        }
    }

    private void OnDestroy()
    {
        ClearPlayedCard();
        if (playedCardCamera != null) Destroy(playedCardCamera.gameObject);
        if (playedCardTexture != null)
        {
            playedCardTexture.Release();
            Destroy(playedCardTexture);
        }
    }

    private IEnumerator OpenAndStartTimer(float duration, Action onTimeOut)
    {
        EnsureCanvasGroup();
        yield return AnimateScroll(true);
        if (isAnswered) yield break;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        transitionCoroutine = null;
        timerCoroutine = StartCoroutine(TimerRoutine(duration, onTimeOut));
    }

    private void OnButtonClicked(int index)
    {
        if (isAnswered) return;
        BattleAudio.Play(BattleSound.AnswerSelect);
        EnsureCanvasGroup();
        isAnswered = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = null;
        Action<int> callback = onOptionSelected;
        onOptionSelected = null;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        transitionCoroutine = StartCoroutine(ClosePanel());
        callback?.Invoke(index);
    }

    private IEnumerator TimerRoutine(float duration, Action onTimeOut)
    {
        EnsureCanvasGroup();
        float remaining = duration;
        duration = Mathf.Max(0f, duration);
        if (timerSlider != null)
        {
            timerSlider.maxValue = duration;
            timerSlider.value = duration;
        }

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            if (timerSlider != null) timerSlider.value = Mathf.Max(0f, remaining);
            yield return null;
        }

        if (!isAnswered)
        {
            isAnswered = true;
            timerCoroutine = null;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            transitionCoroutine = StartCoroutine(ClosePanel());
            onTimeOut?.Invoke();
        }
    }

    private IEnumerator ClosePanel()
    {
        EnsureCanvasGroup();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        yield return AnimateScroll(false);
        ClearPlayedCard();
        transitionCoroutine = null;
        if (gameObject != null && gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void SetPanelOpenState(bool open)
    {
        EnsureCanvasGroup();
        if (scrollRect != null)
            scrollRect.localScale = new Vector3(1f, open ? 1f : 0.02f, 1f);
        canvasGroup.alpha = open ? 1f : 0f;
    }

    private IEnumerator AnimateScroll(bool opening)
    {
        BattleAudio.Play(opening ? BattleSound.QuestionOpen : BattleSound.QuestionClose);
        EnsureCanvasGroup();
        const float duration = 0.48f;
        float elapsed = 0f;
        float from = opening ? 0.02f : 1f;
        float to = opening ? 1f : 0.02f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t * (3f - 2f * t);
            if (scrollRect != null) scrollRect.localScale = new Vector3(1f, Mathf.Lerp(from, to, eased), 1f);
            canvasGroup.alpha = opening ? eased : 1f - eased;
            yield return null;
        }
        if (scrollRect != null) scrollRect.localScale = new Vector3(1f, to, 1f);
        canvasGroup.alpha = opening ? 1f : 0f;
    }

    private static Sprite CreateScrollSprite()
    {
        const int width = 768;
        const int height = 320;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float wave = Mathf.Sin(x * 0.045f) * 2.2f + Mathf.Sin(x * 0.013f + 1.3f) * 2.5f;
                float edge = Mathf.Min(y - (48f + wave), (height - 48f + wave) - y);
                bool inPaper = x >= 22 && x < width - 22 && edge >= 0f;
                bool inRoll = y < 48 || y >= height - 48;
                if (!inPaper && !inRoll) continue;

                float noise = Mathf.PerlinNoise(x * 0.055f, y * 0.075f);
                Color color;
                if (inRoll)
                {
                    int localX = y < 48 ? y : height - 1 - y;
                    float shade = 0.58f + 0.42f * Mathf.Sin((localX / 47f) * Mathf.PI);
                    float grain = (noise - 0.5f) * 0.12f;
                    color = new Color(0.48f * shade + grain + 0.24f,
                        0.27f * shade + grain + 0.22f,
                        0.09f * shade + grain + 0.12f, 1f);
                    if (x < 4 || x >= width - 4) color = new Color(0.23f, 0.12f, 0.045f, 1f);
                    else if (localX < 3 || localX > 44) color = new Color(0.23f, 0.12f, 0.045f, 1f);
                    else if (localX > 8 && localX < 11) color = new Color(0.82f, 0.59f, 0.25f, 1f);
                }
                else
                {
                    bool border = edge < 5f;
                    float shade = Mathf.Lerp(0.82f, 1.04f, Mathf.Clamp01(edge / 75f));
                    float grain = (noise - 0.5f) * 0.09f;
                    color = border
                        ? new Color(0.37f, 0.2f, 0.075f, 1f)
                        : new Color(0.78f * shade + grain, 0.61f * shade + grain, 0.36f * shade + grain, 1f);
                    if (!border && edge > 7f && edge < 10f)
                        color = new Color(0.63f, 0.41f, 0.18f, 1f);
                }

                pixels[y * width + x] = (Color32)color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
