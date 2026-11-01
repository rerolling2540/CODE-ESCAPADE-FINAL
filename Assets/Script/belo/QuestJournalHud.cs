using TMPro;
using UnityEngine;

/// <summary>A fixed HUD icon toggles the scene-authored quest panel.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class QuestJournalHud : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private UnityEngine.UI.Button arrowButton;
    [SerializeField] private TMP_Text objectiveLabel;

    private CanvasGroup visibility;
    private ElfQuestTracker tracker;
    private bool fallbackMinimized = true;
    private string previousObjective;

    void Awake()
    {
        visibility = GetComponent<CanvasGroup>();
        FindTracker();
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
        bool visible = !GameControlsGuide.MapHidden;
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
        if (!visible) return;

        FindTracker();
        ApplyPanelVisibility();
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

    private void ApplyPanelVisibility()
    {
        if (panel == null) return;
        bool open = tracker != null ? !ElfQuestTracker.IsMinimized : !fallbackMinimized;
        if (panel.gameObject.activeSelf != open) panel.gameObject.SetActive(open);
    }

    void OnDestroy()
    {
        if (arrowButton != null) arrowButton.onClick.RemoveListener(ToggleJournal);
    }
}
