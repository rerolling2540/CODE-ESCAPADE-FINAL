using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public CanvasGroup canvasGroup;
    public UnityEngine.UI.Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    public Button[] choiceButtons;
    public bool isDialogueActive;
    private DialogueSO currentDialogue;
    private int dialogueIndex, openedFrame;
    private bool choosing;
    private Coroutine typing;
    public Object Owner { get; private set; }
    public string ActiveLearningVisualProof => isDialogueActive && currentDialogue != null ? currentDialogue.learningVisualProof : null;
    private ActorSO fallbackSpeaker;
    private TMP_FontAsset dialogueFont;
    private Material dialogueMaterial;

    public static DialogueManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (existing != null) { existing.gameObject.SetActive(true); Instance = existing; return existing; }
        var settings = Resources.Load<NPCDialogueSettings>("NPC/SharedDialogueSettings");
        if (settings == null || settings.dialogueCanvasPrefab == null)
        { Debug.LogError("Shared NPC dialogue canvas is not configured."); return null; }
        return Instantiate(settings.dialogueCanvasPrefab).GetComponent<DialogueManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var events = new GameObject("NPC UI EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        if (choiceButtons == null || choiceButtons.Length == 0)
            choiceButtons = GetComponentsInChildren<Button>(true);
        if (choiceButtons.Length == 0)
        {
            // The original prefab contains option artwork; Scene 2 added its Buttons as overrides.
            // Initialize the same authored option boxes when this canvas is used in another scene.
            var choices = transform.Find("Choices");
            var buttons = new System.Collections.Generic.List<Button>();
            if (choices != null)
                foreach (Transform option in choices)
                {
                    var image = option.GetComponent<Image>();
                    if (image == null) continue;
                    var button = option.GetComponent<Button>();
                    if (button == null) button = option.gameObject.AddComponent<Button>();
                    button.targetGraphic = image;
                    buttons.Add(button);
                }
            choiceButtons = buttons.ToArray();
        }
        // The original canvas prefab has a zero scale; keep its authored layout.
        transform.localScale = Vector3.one;
        ConfigureSharedLayout();
        EndDialogue();
    }

    void ConfigureSharedLayout()
    {
        dialogueFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        if (dialogueFont != null)
        {
            dialogueMaterial = new Material(dialogueFont.material) { name = "Shared NPC Clear Text" };
            dialogueMaterial.DisableKeyword("UNDERLAY_ON"); dialogueMaterial.DisableKeyword("UNDERLAY_INNER");
            dialogueMaterial.SetFloat("_OutlineWidth",0); dialogueMaterial.SetFloat("_OutlineSoftness",0); dialogueMaterial.SetFloat("_FaceDilate",0);
        }
        var box = transform.Find("DialogueBox") as RectTransform;
        if (box != null) Place(box, new Vector2(180,120), new Vector2(1450,500));
        var portraitBox = transform.Find("PortraitBox") as RectTransform;
        if (portraitBox != null) Place(portraitBox,new Vector2(-560,110),new Vector2(450,450));
        if (portrait != null && portrait.rectTransform.parent is RectTransform portraitMask &&
            portraitMask.parent == portraitBox)
        {
            // Align the mask with the blue opening above the frame's lower lip.
            Inset(portraitMask, new Vector2(.25f, .28f), new Vector2(.77f, .77f));
            portrait.preserveAspect = true;
        }
        var choices = transform.Find("Choices") as RectTransform;
        if (choices != null)
        {
            choices.anchorMin=choices.anchorMax=new Vector2(.5f,.5f); choices.pivot=new Vector2(.5f,1);
            choices.anchoredPosition=new Vector2(180,-155); choices.sizeDelta=new Vector2(1421,150);
        }
        if (actorName != null)
        {
            ClearText(actorName);
            if (box != null)
            {
                actorName.rectTransform.SetParent(box,false);
                Inset(actorName.rectTransform,new Vector2(.12f,.61f),new Vector2(.94f,.74f));
                var oldNameFrame=transform.Find("ActorName");if(oldNameFrame!=null)oldNameFrame.gameObject.SetActive(false);
            }
            actorName.enableAutoSizing = true;
            actorName.fontSizeMin = 28f; actorName.fontSizeMax = 48f;
            actorName.fontStyle=FontStyles.Bold;actorName.alignment=TextAlignmentOptions.MidlineLeft;
            actorName.margin = Vector4.zero;
        }
        if (dialogueText != null)
        {
            ClearText(dialogueText);
            if(box!=null){dialogueText.rectTransform.SetParent(box,false);Inset(dialogueText.rectTransform,new Vector2(.12f,.24f),new Vector2(.94f,.60f));}
            dialogueText.enableAutoSizing = true;
            dialogueText.fontSizeMin = 26f; dialogueText.fontSizeMax = 34f;
            dialogueText.fontStyle=FontStyles.Normal;dialogueText.alignment=TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.margin = Vector4.zero;dialogueText.lineSpacing=2f;
        }
    }
    void ClearText(TMP_Text text)
    {
        if(dialogueFont!=null)text.font=dialogueFont;
        if(dialogueMaterial!=null)text.fontSharedMaterial=dialogueMaterial;
        text.color=new Color(.20f,.11f,.05f);
    }
    static void Place(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
    static void Inset(RectTransform rect,Vector2 min,Vector2 max)
    {rect.anchorMin=min;rect.anchorMax=max;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;}

    void FitPortrait(Sprite sprite)
    {
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        if (sprite == null || !(portrait.rectTransform.parent is RectTransform mask)) return;

        // Match Image's sprite padding so transparent margins don't shift or shrink the artwork.
        Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
        float left = Mathf.Round(padding.x), bottom = Mathf.Round(padding.y);
        float right = Mathf.Round(padding.z), top = Mathf.Round(padding.w);
        Vector2 spriteSize = sprite.rect.size;
        float width = Mathf.Max(1f, spriteSize.x - left - right);
        float height = Mathf.Max(1f, spriteSize.y - bottom - top);
        float scale = Mathf.Min(mask.rect.width / width, mask.rect.height / height);
        Vector2 offset = new Vector2(right - left, top - bottom) * (scale * .5f);
        Place(portrait.rectTransform, offset, spriteSize * scale);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if(dialogueMaterial!=null){if(Application.isPlaying)Destroy(dialogueMaterial);else DestroyImmediate(dialogueMaterial);}
    }

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
        if (!isDialogueActive || Time.frameCount == openedFrame || Time.timeScale <= 0f || GameControlsGuide.GameplayOverlayHidden) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { EndDialogue(); return; }
        bool advance = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        advance |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        advance |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (advance && !choosing) AdvanceDialogue();
    }

    public void StartDialogue(DialogueSO dialogueSO, Object owner = null, ActorSO speaker = null)
    {
        if (dialogueSO == null) return;
        StopTyping();
        ClearChoiceButtons();
        currentDialogue = dialogueSO;
        Owner = owner;
        fallbackSpeaker = speaker;
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
        var speaker = line.speaker != null ? line.speaker : fallbackSpeaker;
        if (speaker != null)
        {
            FitPortrait(speaker.portrait);
            actorName.text = speaker.actorName;
        }
        else { portrait.enabled = false; actorName.text = "Traveller"; }
        dialogueText.text = MapSceneNames.DisplayText(line.text) + "\n<size=75%><b><color=#342014>[Left click / E / Space] Continue    [Esc] Close</color></b></size>";
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
                var source = currentDialogue;
                var owner = Owner;
                var speaker = fallbackSpeaker;
                option.onSelected?.Invoke();
                // A service may close dialogue or replace it; never close a newly opened conversation.
                if (!isDialogueActive || currentDialogue != source) return;
                if (option.nextDialogue != null) StartDialogue(option.nextDialogue, owner, speaker);
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
        // Full-width rows leave room for quest and service names at a readable size.
        int columns = 1;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(0f, 10f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        float width = Mathf.Min(720f, area.rect.width - grid.padding.horizontal);
        float height = Mathf.Min(100f, (320f - grid.padding.vertical - (count - 1) * 10f) / count);
        grid.cellSize = new Vector2(Mathf.Max(160f, width), height);
        int rows = Mathf.CeilToInt((float)count / columns);
        area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rows * height + (rows - 1) * 10f + grid.padding.vertical);
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
        var frame = button.targetGraphic as Image;
        var choiceFrame = Resources.Load<Sprite>("UI/NPC/DialogueChoice");
        if (frame != null && choiceFrame != null)
        {
            frame.sprite = choiceFrame;
            frame.type = Image.Type.Sliced;
            frame.preserveAspect = false;
            // Tiny Swords uses 64 PPU against a 100-PPU canvas. At 1x the
            // sliced top/bottom borders exceed the row height and crush its fill.
            frame.pixelsPerUnitMultiplier = 4f;
        }
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            ClearText(label);label.color=new Color(1f,.91f,.69f);label.fontStyle=FontStyles.Bold;
            // Replace the authored asymmetric offsets/margins with an inset
            // text area that follows the option box at every canvas scale.
            var rect = label.rectTransform;
            if (rect != button.transform)
            {
                // The sliced frame keeps fixed corners and a broad text area.
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(48f, 24f);
                rect.offsetMax = new Vector2(-48f, -20f);
                rect.localScale = Vector3.one;
            }
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 32f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.text = MapSceneNames.DisplayText(text);
        }
        button.gameObject.SetActive(true);
        button.onClick.AddListener(action);
        if (index == 0) button.Select();
    }

    public void EndDialogue()
    {
        StopTyping();
        isDialogueActive = false;
        Owner = null;
        currentDialogue = null;
        fallbackSpeaker = null;
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
