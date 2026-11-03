using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleMessageUI : MonoBehaviour
{
    public static BattleMessageUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 2f;

    private Coroutine currentRoutine;
    private RectTransform panelRect;
    private Vector3 floatingWorldPosition;
    private bool isFloating;
    private Vector2 defaultAnchoredPosition;
    private readonly Queue<FloatingMessage> queuedMessages = new();

    private readonly struct FloatingMessage
    {
        public readonly string Title;
        public readonly string Description;
        public readonly Vector3 Position;

        public FloatingMessage(string title, string description, Vector3 position)
        {
            Title = title;
            Description = description;
            Position = position;
        }
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;
        if (panelRect != null) defaultAnchoredPosition = panelRect.anchoredPosition;
        if (panel != null)
            BattleUITheme.DecoratePanel(panel.GetComponent<UnityEngine.UI.Image>());
        if (titleText != null)
        {
            titleText.color = new Color(1f, 0.87f, 0.57f);
            titleText.fontStyle = FontStyles.Bold;
        }
        if (descriptionText != null)
            descriptionText.color = new Color(0.95f, 0.87f, 0.7f);

        // This component lives on the active Canvas; only its child display panel is hidden.
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string title, string description)
    {
        // Stop previous message if one is already showing
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        isFloating = false;
        if (panelRect != null) panelRect.anchoredPosition = defaultAnchoredPosition;
        currentRoutine = StartCoroutine(ShowRoutine(title, description));
    }

    /// <summary>Shows a brief action callout above a combatant in world space.</summary>
    public void ShowAt(string title, string description, Vector3 worldPosition)
    {
        if (panel == null || panelRect == null || titleText == null || descriptionText == null) return;
        if (currentRoutine != null)
        {
            queuedMessages.Enqueue(new FloatingMessage(title, description, worldPosition));
            return;
        }
        floatingWorldPosition = worldPosition + Vector3.up * 1.1f;
        isFloating = true;
        currentRoutine = StartCoroutine(ShowRoutine(title, description));
    }

    private IEnumerator ShowRoutine(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;

        panel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            if (isFloating) UpdateFloatingPosition(elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        panel.SetActive(false);
        isFloating = false;
        if (panelRect != null) panelRect.anchoredPosition = defaultAnchoredPosition;

        currentRoutine = null;
        if (queuedMessages.Count > 0)
        {
            FloatingMessage next = queuedMessages.Dequeue();
            ShowAt(next.Title, next.Description, next.Position);
        }
    }

    private void UpdateFloatingPosition(float elapsed)
    {
        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        if (canvas == null || panelRect.parent is not RectTransform parentRect) return;

        Camera worldCamera = Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, floatingWorldPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint)) return;

        // Rise gently while keeping the callout inside the visible canvas.
        localPoint.y += Mathf.Clamp01(elapsed / Mathf.Max(0.01f, displayDuration)) * 28f;
        Rect canvasRect = parentRect.rect;
        Vector2 halfSize = panelRect.rect.size * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x, canvasRect.xMin + halfSize.x, canvasRect.xMax - halfSize.x);
        localPoint.y = Mathf.Clamp(localPoint.y, canvasRect.yMin + halfSize.y, canvasRect.yMax - halfSize.y);
        panelRect.anchoredPosition = localPoint;
    }
}
