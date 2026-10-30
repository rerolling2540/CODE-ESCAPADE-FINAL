using UnityEngine;

[RequireComponent(typeof(CaveEnemyEncounter), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class DemonSlimeTransformation : MonoBehaviour
{
    public Sprite[] dormantFrames;
    [Min(1f)] public float activationDistance = 6f;
    [Min(0.1f)] public float transformationDuration = 1.2f;
    [Min(0.1f)] public float dormantHeight = 1.8f;
    public bool dormantFacesLeft;
    private CaveEnemyEncounter encounter;
    private SpriteRenderer visual;
    private BoxCollider2D hitbox;
    private playerMovement player;
    private Vector3 demonScale;
    private float elapsed;
    private bool transforming;
    public bool Transformed { get; private set; }

    void Awake()
    {
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved?.defeatedEncounters != null && saved.defeatedEncounters.Contains(gameObject.name))
        {
            gameObject.SetActive(false);
            return;
        }
        encounter = GetComponent<CaveEnemyEncounter>();
        visual = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        demonScale = transform.localScale;
        encounter.enabled = false;
        hitbox.enabled = false;
        ShowDormant();
    }

    void Update()
    {
        if (Transformed || Time.timeScale <= 0f || GameControlsGuide.GameplayOverlayHidden ||
            (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)) return;
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (!transforming)
        {
            ShowDormant();
            if (player == null || Vector2.Distance(player.transform.position, transform.position) > activationDistance) return;
            BeginTransformation();
        }
        elapsed += Time.deltaTime;
        ApplyTransformation(Mathf.Clamp01(elapsed / transformationDuration));
    }

    void ShowDormant()
    {
        if (dormantFrames == null || dormantFrames.Length == 0) return;
        visual.sprite = dormantFrames[Mathf.FloorToInt(Time.time * 8f) % dormantFrames.Length];
        transform.localScale = Vector3.one * (dormantHeight / Mathf.Max(.1f, dormantFrames[0].bounds.size.y));
    }

    void LateUpdate()
    {
        if (Time.timeScale <= 0f || GameControlsGuide.GameplayOverlayHidden || visual == null) return;
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (player == null) return;
        if (transforming || Transformed)
        {
            encounter.FaceToward(player.transform.position);
            return;
        }
        float delta = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(delta) > .01f)
            visual.flipX = (delta * transform.TransformVector(Vector3.right).x < 0f) ^ dormantFacesLeft;
    }

    public void BeginTransformation()
    {
        if (Transformed || transforming || encounter == null || encounter.enemy == null) return;
        transforming = true;
        elapsed = 0f;
        ApplyTransformation(0f);
    }

    void ApplyTransformation(float progress)
    {
        var frames = encounter.enemy.idleFrames;
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt(elapsed * encounter.enemy.battleFramesPerSecond) % frames.Length];
        transform.localScale = demonScale * Mathf.Lerp(.3f, 1f, Mathf.SmoothStep(0f, 1f, progress));
        if (progress < 1f) return;
        Transformed = true;
        hitbox.enabled = true;
        encounter.enabled = true;
    }
}
