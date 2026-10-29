using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DoorJavaQuestionPanel : MonoBehaviour
{
    private static DoorJavaQuestionPanel instance;
    private TMP_Text questionLabel, codeLabel, feedback;
    private readonly UnityEngine.UI.Button[] answers = new UnityEngine.UI.Button[4];
    private readonly TMP_Text[] answerLabels = new TMP_Text[4];
    private MultipleChoiceQuestionData question;
    private MultipleChoiceQuestionData[] questionPool;
    private string currentAttemptId;
    private Action accepted, cancelled;
    private float previousTimeScale;
    private bool open;
    private bool trapPuzzle;
    private ExplorationHUD[] hiddenHuds;

    public static void Show(MultipleChoiceQuestionData[] data, Action onAccepted, Action onCancelled, bool forTrap = false)
    {
        var pool = data == null ? Array.Empty<MultipleChoiceQuestionData>() : Array.FindAll(data, q => q != null);
        if (pool.Length == 0) { onCancelled?.Invoke(); return; }
        if (instance == null) Create();
        instance.questionPool = pool;
        instance.trapPuzzle=forTrap;
        instance.accepted = onAccepted;
        instance.cancelled = onCancelled;
        instance.previousTimeScale = Time.timeScale;
        instance.open = true;
        instance.hiddenHuds = Array.FindAll(FindObjectsByType<ExplorationHUD>(FindObjectsSortMode.None), hud => hud.enabled);
        foreach (var hud in instance.hiddenHuds) hud.enabled = false;
        Time.timeScale = 0f;
        instance.gameObject.SetActive(true);
        instance.PickQuestion(forTrap ? "Solve the coding puzzle to permanently disable this trap." : "Answer correctly to open the door, then walk into the boss chamber.");
        BattleAudio.EnsureInstance();
        BattleAudio.Play(BattleSound.QuestionOpen);
    }

    void PickQuestion(string message)
    {
        var alternatives = Array.FindAll(questionPool, q => q != question);
        var choices = alternatives.Length > 0 ? alternatives : questionPool;
        question = choices[UnityEngine.Random.Range(0, choices.Length)];
        currentAttemptId = QuestionPerformanceTracker.Begin(question, "door", "java");
        questionLabel.text = question.questionText;
        codeLabel.text = question.codeSnippet;
        feedback.text = message;
        for (int i = 0; i < 4; i++)
        {
            bool available = question.options != null && i < question.options.Length;
            answers[i].gameObject.SetActive(available);
            if (available) answerLabels[i].text = $"{(char)('A' + i)}. {question.options[i]}";
        }
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(answers[0].gameObject);
    }

    public void SelectAnswer(int index)
    {
        if (!open || question == null) return;
        BattleAudio.Play(BattleSound.AnswerSelect);
        if (!question.CheckAnswer(index))
        {
            QuestionPerformanceTracker.Complete(currentAttemptId, false);
            currentAttemptId = null;
            PickQuestion(trapPuzzle ? "Incorrect. The trap is still active. Try a different question." : "Incorrect. The door remains closed. Try a different question.");
            return;
        }
        QuestionPerformanceTracker.Complete(currentAttemptId, true);
        currentAttemptId = null;
        var callback = accepted;
        Close();
        callback?.Invoke();
    }

    public void Cancel()
    {
        if (!open) return;
        QuestionPerformanceTracker.Abandon(currentAttemptId);
        currentAttemptId = null;
        var callback = cancelled;
        Close();
        callback?.Invoke();
    }

    void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
    }

    void Close()
    {
        open = false;
        Time.timeScale = previousTimeScale;
        RestoreHud();
        BattleAudio.Play(BattleSound.QuestionClose);
        accepted = cancelled = null;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (open) { Time.timeScale = previousTimeScale; RestoreHud(); }
        if (instance == this) instance = null;
    }

    void RestoreHud()
    {
        if (hiddenHuds == null) return;
        foreach (var hud in hiddenHuds) if (hud != null) hud.enabled = true;
        hiddenHuds = null;
    }

    static void Create()
    {
        var root = new GameObject("Demon Door Java Question", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        root.SetActive(false);
        instance = root.AddComponent<DoorJavaQuestionPanel>();
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var shade = Box("Modal Backdrop", root.transform, Vector2.zero, new Vector2(1280, 720), new Color(0, 0, 0, 0.78f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
        var panel = Box("Question Panel", root.transform, Vector2.zero, new Vector2(960, 600), new Color(0.13f, 0.085f, 0.06f));
        var border = panel.gameObject.AddComponent<UnityEngine.UI.Outline>();
        border.effectColor = new Color(0.73f, 0.52f, 0.22f); border.effectDistance = new Vector2(3, -3);
        Label("Title", panel, new Vector2(0, 248), new Vector2(850, 46), "THE DEMON'S CODE CHALLENGE", 30);
        instance.questionLabel = Label("Question", panel, new Vector2(0, 174), new Vector2(850, 84), "", 26);
        var code = Box("Code", panel, new Vector2(0, 85), new Vector2(840, 70), new Color(0.035f, 0.03f, 0.025f));
        instance.codeLabel = Label("Java Code", code, Vector2.zero, new Vector2(800, 62), "", 27);
        for (int i = 0; i < 4; i++)
        {
            int choice = i;
            Vector2 position = new Vector2(i % 2 == 0 ? -215 : 215, i < 2 ? -8 : -82);
            var button = Button("Answer " + (char)('A' + i), panel, position, new Vector2(410, 60), "");
            instance.answers[i] = button;
            instance.answerLabels[i] = button.GetComponentInChildren<TMP_Text>();
            button.onClick.AddListener(() => instance.SelectAnswer(choice));
        }
        instance.feedback = Label("Feedback", panel, new Vector2(0, -169), new Vector2(840, 78), "", 22);
        Button("Cancel", panel, new Vector2(0, -250), new Vector2(260, 48), "Leave door [Esc]").onClick.AddListener(instance.Cancel);
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var events = new GameObject("Door Question EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            events.transform.SetParent(root.transform, false);
        }
    }

    static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        item.GetComponent<UnityEngine.UI.Image>().color = color;
        return rect;
    }

    static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = item.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = item.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.9f, 0.68f); label.raycastTarget = false;
        return label;
    }

    static UnityEngine.UI.Button Button(string name, Transform parent, Vector2 position, Vector2 size, string text)
    {
        var rect = Box(name, parent, position, size, new Color(0.3f, 0.2f, 0.11f));
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.3f, 1.2f, 0.85f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Label("Text", rect, Vector2.zero, size - new Vector2(20, 4), text, 24);
        return button;
    }
}
