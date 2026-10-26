using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public CanvasGroup canvasGroup;
    public Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    public Button[] choiceButtons;
    public bool isDialogueActive;
    private DialogueSO currentDialogue;
    private int dialogueIndex, openedFrame;
    private bool choosing;
    private Coroutine typing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (choiceButtons == null || choiceButtons.Length == 0)
            choiceButtons = GetComponentsInChildren<Button>(true);
        // The original canvas prefab has a zero scale; keep its authored layout.
        transform.localScale = Vector3.one;
        EndDialogue();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    void LateUpdate()
    {
        if (!isDialogueActive || canvasGroup == null) return;
        bool visible = !GameControlsGuide.GameplayOverlayHidden;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void Update()
    {
        if (!isDialogueActive || Time.frameCount == openedFrame || Time.timeScale <= 0f) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { EndDialogue(); return; }
        bool advance = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        advance |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (advance && !choosing) AdvanceDialogue();
    }

    public void StartDialogue(DialogueSO dialogueSO)
    {
        if (dialogueSO == null) return;
        StopTyping();
        ClearChoiceButtons();
        currentDialogue = dialogueSO;
        dialogueIndex = 0;
        choosing = false;
        openedFrame = Time.frameCount;
        isDialogueActive = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        ShowDialogue();
    }

    public void AdvanceDialogue()
    {
        if (!isDialogueActive || choosing || currentDialogue == null) return;
        if (typing != null)
        {
            StopTyping();
            dialogueText.maxVisibleCharacters = int.MaxValue;
            return;
        }
        ShowDialogue();
    }

    private void ShowDialogue()
    {
        if (currentDialogue.lines == null || dialogueIndex >= currentDialogue.lines.Length)
        { ShowChoices(); return; }
        var line = currentDialogue.lines[dialogueIndex++];
        if (line.speaker != null)
        {
            portrait.sprite = line.speaker.portrait;
            portrait.enabled = portrait.sprite != null;
            actorName.text = line.speaker.actorName;
        }
        dialogueText.text = line.text + "\n<size=75%><b><color=#342014>[E / Space] Continue    [Esc] Close</color></b></size>";
        dialogueText.maxVisibleCharacters = 0;
        typing = StartCoroutine(RevealText());
    }

    IEnumerator RevealText()
    {
        dialogueText.ForceMeshUpdate();
        int count = dialogueText.textInfo.characterCount;
        for (int i = 0; i <= count; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(0.018f);
        }
        typing = null;
    }

    private void ShowChoices()
    {
        ClearChoiceButtons();
        choosing = true;
        int count = currentDialogue.options == null ? 0 : currentDialogue.options.Length;
        if (count == 0)
        {
            SetChoice(0, "Farewell", EndDialogue);
            LayoutChoices(1);
            return;
        }
        for (int i = 0; i < Mathf.Min(count, choiceButtons.Length); i++)
        {
            var option = currentDialogue.options[i];
            SetChoice(i, option.optionText, () =>
            {
                if (!choosing) return;
                choosing = false;
                option.onSelected?.Invoke();
                if (option.nextDialogue != null) StartDialogue(option.nextDialogue);
                else EndDialogue();
            });
        }
        LayoutChoices(Mathf.Min(count, choiceButtons.Length));
    }

    void LayoutChoices(int count)
    {
        if (count == 0 || choiceButtons[0] == null) return;
        var grid = choiceButtons[0].GetComponentInParent<GridLayoutGroup>();
        if (grid == null) return;
        var area = (RectTransform)grid.transform;
        int columns = Mathf.Min(2, count);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(24f, 16f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        float width = Mathf.Min(450f, (area.rect.width - grid.padding.horizontal - 24f * (columns - 1)) / columns);
        grid.cellSize = new Vector2(Mathf.Max(160f, width), 150f);
        int rows = Mathf.CeilToInt((float)count / columns);
        area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rows * 150f + (rows - 1) * 16f + grid.padding.vertical);
        LayoutRebuilder.ForceRebuildLayoutImmediate(area);
        foreach (var button in choiceButtons)
        {
            if (button == null || !button.gameObject.activeSelf) continue;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.ForceMeshUpdate();
        }
    }

    void SetChoice(int index, string text, UnityEngine.Events.UnityAction action)
    {
        if (choiceButtons == null || index >= choiceButtons.Length || choiceButtons[index] == null) return;
        var button = choiceButtons[index];
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            // Replace the authored asymmetric offsets/margins with an inset
            // text area that follows the option box at every canvas scale.
            var rect = label.rectTransform;
            if (rect != button.transform)
            {
                // This sprite includes transparent space and a thick frame.
                // Inset proportionally so the text fits the painted blue area.
                rect.anchorMin = new Vector2(0.23f, 0.25f);
                rect.anchorMax = new Vector2(0.77f, 0.78f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 28f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.text = text;
        }
        button.gameObject.SetActive(true);
        button.onClick.AddListener(action);
        if (index == 0) button.Select();
    }

    public void EndDialogue()
    {
        StopTyping();
        isDialogueActive = false;
        choosing = false;
        dialogueIndex = 0;
        ClearChoiceButtons();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void StopTyping()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
    }

    private void ClearChoiceButtons()
    {
        if (choiceButtons == null) return;
        foreach (var button in choiceButtons)
        {
            if (button == null) continue;
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }
    }
}
