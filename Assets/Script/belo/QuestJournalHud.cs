using TMPro;
using UnityEngine;

/// <summary>Scene-authored quest drawer. Its saved position is the closed position.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class QuestJournalHud : MonoBehaviour
{
    [SerializeField] private RectTransform slideRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private UnityEngine.UI.Button arrowButton;
    [SerializeField] private RectTransform arrowArtwork;
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField, Min(0.05f)] private float slideDuration = 0.25f;

    private CanvasGroup visibility;
    private ElfQuestTracker tracker;
    private Vector2 closedPosition;
    private Vector3 arrowScale;
    private float reveal;
    private string previousObjective;

    void Awake()
    {
        visibility = GetComponent<CanvasGroup>();
        if (slideRoot != null) closedPosition = slideRoot.anchoredPosition;
        if (arrowArtwork != null) arrowScale = arrowArtwork.localScale;
        reveal = ElfQuestTracker.IsMinimized ? 0f : 1f;
        if (arrowButton != null) arrowButton.onClick.AddListener(ToggleJournal);
        ApplySlide();
    }

    public void ToggleJournal()
    {
        if (GameControlsGuide.MapHidden) return;
        FindTracker();
        if (tracker != null) tracker.ToggleJournal();
    }

    void Update()
    {
        bool visible = !GameControlsGuide.MapHidden;
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
        if (!visible) return;

        FindTracker();
        reveal = Mathf.MoveTowards(reveal, ElfQuestTracker.IsMinimized ? 0f : 1f,
            Mathf.Min(Time.unscaledDeltaTime, 0.05f) / slideDuration);
        ApplySlide();
        string objective = tracker != null ? tracker.ObjectiveText : "Speak to the elf at the cave entrance [E].";
        if (objectiveLabel != null && objective != previousObjective)
        {
            objectiveLabel.text = objective;
            previousObjective = objective;
        }
    }

    private void FindTracker()
    {
        if (tracker == null) tracker = FindFirstObjectByType<ElfQuestTracker>();
    }

    private void ApplySlide()
    {
        if (slideRoot == null || panel == null) return;
        float distance = panel.rect.width + Mathf.Max(0f, panel.anchoredPosition.x);
        slideRoot.anchoredPosition = closedPosition + Vector2.left * (distance * Mathf.SmoothStep(0f, 1f, reveal));
        if (arrowArtwork != null)
            arrowArtwork.localScale = new Vector3(arrowScale.x * (ElfQuestTracker.IsMinimized ? 1f : -1f), arrowScale.y, arrowScale.z);
    }

    void OnDestroy()
    {
        if (arrowButton != null) arrowButton.onClick.RemoveListener(ToggleJournal);
    }
}
