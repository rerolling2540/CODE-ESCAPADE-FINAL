using TMPro;
using UnityEngine;

/// <summary>Supports both a fixed quest icon and a scene-authored sliding drawer.</summary>
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
    private bool fallbackMinimized = true;
    private Vector2 closedPosition;
    private Vector3 arrowScale;
    private float reveal;
    private string previousObjective;
    private float originalPanelHeight,originalTextHeight;
    private Vector2 originalTextPosition;

    void Awake()
    {
        visibility = GetComponent<CanvasGroup>();
        foreach(var label in GetComponentsInChildren<TMP_Text>(true))if(label.name=="Quest Name")label.text="Learning & Cave Quests";
        if(panel!=null)originalPanelHeight=panel.rect.height;
        if(objectiveLabel!=null){originalTextHeight=objectiveLabel.rectTransform.rect.height;originalTextPosition=objectiveLabel.rectTransform.anchoredPosition;}
        if (slideRoot != null) closedPosition = slideRoot.anchoredPosition;
        if (arrowArtwork != null) arrowScale = arrowArtwork.localScale;
        FindTracker();
        reveal = IsMinimized ? 0f : 1f;
        if (arrowButton != null) arrowButton.onClick.AddListener(ToggleJournal);
        ApplyPanelVisibility();
    }

    public void ToggleJournal()
    {
        if (GameControlsGuide.GameplayOverlayHidden || GameControlsGuide.MapHidden) return;
        FindTracker();
        if (tracker != null) tracker.ToggleJournal();
        else fallbackMinimized = !fallbackMinimized;
        ApplyPanelVisibility();
    }

    void Update()
    {
        bool visible = !GameControlsGuide.GameplayOverlayHidden && !GameControlsGuide.MapHidden;
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
        if (!visible) return;

        FindTracker();
        reveal = Mathf.MoveTowards(reveal, IsMinimized ? 0f : 1f,
            Mathf.Min(Time.unscaledDeltaTime, 0.05f) / Mathf.Max(0.05f, slideDuration));
        ApplyPanelVisibility();
        string objective = tracker != null ? tracker.ObjectiveText : "Speak to the elf at the cave entrance [E].";
        if (objectiveLabel != null && objective != previousObjective)
        {
            objectiveLabel.text = objective;
            if(panel!=null)
            {
                float height=Mathf.Max(originalTextHeight,objectiveLabel.GetPreferredValues(objective,objectiveLabel.rectTransform.rect.width,Mathf.Infinity).y+12);
                float extra=height-originalTextHeight;
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,originalPanelHeight+extra);
                objectiveLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
                objectiveLabel.rectTransform.anchoredPosition=originalTextPosition+Vector2.down*extra*(1-objectiveLabel.rectTransform.pivot.y);
            }
            previousObjective = objective;
        }
    }

    private void FindTracker()
    {
        if (tracker == null) tracker = FindFirstObjectByType<ElfQuestTracker>();
    }

    private bool IsMinimized => tracker != null ? ElfQuestTracker.IsMinimized : fallbackMinimized;

    private void ApplyPanelVisibility()
    {
        if (panel == null) return;
        if (slideRoot != null)
        {
            // Existing authored drawers keep their slide and arrow animation.
            // Fixed-icon prefabs leave slideRoot unassigned.
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            ApplySlide();
            return;
        }
        bool open = !IsMinimized;
        if (panel.gameObject.activeSelf != open) panel.gameObject.SetActive(open);
    }

    private void ApplySlide()
    {
        if (slideRoot == null || panel == null) return;
        float distance = panel.rect.width + Mathf.Max(0f, panel.anchoredPosition.x);
        slideRoot.anchoredPosition = closedPosition + Vector2.left * (distance * Mathf.SmoothStep(0f, 1f, reveal));
        if (arrowArtwork != null)
            arrowArtwork.localScale = new Vector3(arrowScale.x * (IsMinimized ? 1f : -1f), arrowScale.y, arrowScale.z);
    }

    void OnDestroy()
    {
        if (arrowButton != null) arrowButton.onClick.RemoveListener(ToggleJournal);
    }
}
