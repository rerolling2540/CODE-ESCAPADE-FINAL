using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class DemonDoor : MonoBehaviour
{
    public MultipleChoiceQuestionData entranceQuestion;
    public MultipleChoiceQuestionData[] entranceQuestions;
    public UnityEngine.BoxCollider2D arenaBarrier;
    [Tooltip("Portal inside the doorway, revealed after the opening animation finishes.")]
    public GameObject revealedPortal;
    public Sprite closedFrame;
    public Sprite[] glowFrames;
    public Sprite[] openingFrames;
    [Min(1f)] public float framesPerSecond = 8f;

    private SpriteRenderer visual;
    private bool entering;
    public bool IsOpened => entering;
    private bool asking;
    private float floorY;

    void Awake()
    {
        if (revealedPortal != null) revealedPortal.SetActive(false);
    }

    void Start()
    {
        visual = GetComponent<SpriteRenderer>();
        floorY = visual.bounds.min.y;
        var interaction = PortalInteraction.For(gameObject);
        interaction.entryPrompt = "[E] Enter door";
        interaction.unavailablePrompt = "";
        interaction.playEntrySound = false;
        interaction.AddRoute(() => isActiveAndEnabled && !entering && !asking &&
            HasQuestions(), AskQuestion);
    }

    void Update()
    {
        if (entering) return;
        SetFrame(glowFrames != null && glowFrames.Length > 0
            ? glowFrames[Mathf.FloorToInt(Time.time * 4f) % glowFrames.Length] : closedFrame);
    }

    void AskQuestion(Transform player)
    {
        asking = true;
        var questions = QuestionSystem.Instance != null
            ? QuestionSystem.Instance.GetQuestionsForCurrentPlayer()
            : System.Array.Empty<MultipleChoiceQuestionData>();
        if (questions.Length == 0)
            questions = entranceQuestions != null && System.Array.Exists(entranceQuestions, q => q != null)
                ? entranceQuestions : new[] { entranceQuestion };
        DoorJavaQuestionPanel.Show(questions, () =>
        {
            asking = false;
            if (this != null) StartCoroutine(Enter());
        }, () => { if (this != null) asking = false; });
    }

    bool HasQuestions() => entranceQuestion != null ||
        (entranceQuestions != null && System.Array.Exists(entranceQuestions, q => q != null));

    void SetFrame(Sprite frame)
    {
        if (frame == null) return;
        visual.sprite = frame;
        var position = transform.position;
        position.y = floorY - frame.bounds.min.y * transform.lossyScale.y;
        transform.position = position;
    }

    IEnumerator Enter()
    {
        entering = true;
        PortalEntrySound.PlayFor(gameObject);
        if (openingFrames != null)
            foreach (var frame in openingFrames)
            {
                SetFrame(frame);
                yield return new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
            }
        if (arenaBarrier != null) arenaBarrier.enabled = false;
        if (revealedPortal != null) revealedPortal.SetActive(true);
    }
}
