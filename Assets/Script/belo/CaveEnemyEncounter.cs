using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class CaveEnemyEncounter : MonoBehaviour
{
    public EnemyData enemy;
    [Tooltip("Optional battle squad. The overworld still displays this encounter as the single enemy above.")]
    public EncounterData battleEncounter;
    [Min(0.1f), Tooltip("Distance at which the player can trigger this encounter without touching it.")]
    public float interactionDistance = 2.5f;
    [Min(0.1f), Tooltip("Avoid triggering encounters on distant floors above or below the player.")]
    public float maxEncounterHeightDifference = 3.5f;
    public bool flying;
    [Min(0f)] public float patrolSpeed = 1.2f;
    [Min(0.5f)] public float overworldHeight = 1.9f;
    [Min(0.5f)] public float patrolRadius = 3f;
    [Tooltip("Keep the size and position authored in the scene when the encounter starts.")]
    public bool preserveAuthoredTransform;
    [Tooltip("Keep the sprite's authored facing direction while patrolling or idling.")]
    public bool lockFacingDirection;
    [Tooltip("Enable for artwork that faces left before SpriteRenderer flipping.")]
    public bool spriteFacesLeft;
    [Tooltip("Keep the authored encounter hitbox instead of including transparent animation padding.")]
    public bool preserveAuthoredHitbox;
    public static List<EnemyData> PendingEnemies;
    public static string PendingEncounterId;
    private static string suppressedEncounter;
    private static bool enteringBattle;
    private playerMovement player;
    private Vector3 restingPosition;
    private SpriteRenderer visual;
    private Rigidbody2D body;
    private float direction = 1f;
    private float readyAt;

    void Start()
    {
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved?.defeatedEncounters != null && saved.defeatedEncounters.Contains(gameObject.name))
        { gameObject.SetActive(false); return; }
        enteringBattle = false;
        readyAt = Time.unscaledTime + 2f;
        player = FindFirstObjectByType<playerMovement>();
        visual = GetComponent<SpriteRenderer>();
        if (enemy != null && enemy.Image != null)
        {
            visual.sprite = enemy.Image;
            if (!preserveAuthoredTransform)
                transform.localScale = Vector3.one * (overworldHeight / Mathf.Max(0.1f, enemy.Image.bounds.size.y));
            if (enemy.battleMaterial != null) visual.sharedMaterial = enemy.battleMaterial;
        }
        // Anchor the enlarged sprite to the actual cave floor.
        if (!preserveAuthoredTransform && FindGround((Vector2)transform.position + Vector2.up, 8f, out var floor))
        {
            float offset = visual.sprite != null ? -visual.sprite.bounds.min.y * transform.localScale.y : 0f;
            transform.position = new Vector3(transform.position.x, floor.point.y + offset + (flying ? 1.1f : 0.03f), transform.position.z);
        }
        restingPosition = transform.position;
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var trigger = GetComponent<BoxCollider2D>();
        if (trigger == null) trigger = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        if (visual.sprite != null && !preserveAuthoredHitbox)
        {
            trigger.size = visual.sprite.bounds.size;
            trigger.offset = visual.sprite.bounds.center;
        }
    }

    static bool FindGround(Vector2 origin, float distance, out RaycastHit2D ground)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, distance))
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.CompareTag("Ground") && hit.normal.y > 0.5f)
            { ground = hit; return true; }
        ground = default;
        return false;
    }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (enemy != null)
        {
            var frames = !enteringBattle && patrolSpeed > 0f && enemy.walkFrames != null && enemy.walkFrames.Length > 0
                ? enemy.walkFrames : enemy.idleFrames;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames[Mathf.FloorToInt(Time.time * 8f) % frames.Length];
            if (!flying && patrolSpeed <= 0f && visual.sprite != null && enemy.Image != null)
            {
                // Cropped idle frames must keep the stationary boss on the floor.
                Vector2 position = body.position;
                position.y = restingPosition.y + (enemy.Image.bounds.min.y - visual.sprite.bounds.min.y) * transform.localScale.y;
                body.position = position;
            }
        }
        if (player != null)
        {
            if (suppressedEncounter == gameObject.name && !IsPlayerInRange(player, 1.5f, false))
                suppressedEncounter = null;
            if (IsPlayerInRange(player)) TryStartBattle(player);
        }
    }

    void FixedUpdate()
    {
        if (body == null || enteringBattle) return;
        if (patrolSpeed <= 0f)
        {
            if (!lockFacingDirection && player != null) FaceToward(player.transform.position);
            return;
        }
        float ahead = body.position.x + direction * 0.7f;
        bool turn = Mathf.Abs(ahead - restingPosition.x) >= patrolRadius;
        float feet = visual.bounds.min.y;
        if (!flying)
        {
            if (!FindGround(new Vector2(ahead, feet + 0.6f), 1f, out var ground) || ground.point.y < feet - 0.3f) turn = true;
            foreach (var hit in Physics2D.RaycastAll(new Vector2(body.position.x, feet + 0.7f), Vector2.right * direction, 0.7f))
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject &&
                    hit.collider.GetComponentInParent<playerMovement>() == null) turn = true;
        }
        if (turn) direction = -direction;
        if (!lockFacingDirection) visual.flipX = (direction < 0f) ^ spriteFacesLeft;
        Vector2 next = body.position + Vector2.right * (direction * patrolSpeed * Time.fixedDeltaTime);
        if (flying) next.y = restingPosition.y + Mathf.Sin(Time.time * 2f) * 0.15f;
        body.MovePosition(next);
    }

    public void FaceToward(Vector3 target)
    {
        if (lockFacingDirection) return;
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        float delta = target.x - transform.position.x;
        if (Mathf.Abs(delta) < .01f) return;
        visual.flipX = (delta * transform.TransformVector(Vector3.right).x < 0f) ^ spriteFacesLeft;
    }

    void OnTriggerEnter2D(Collider2D other) { TryStartBattle(other.GetComponentInParent<playerMovement>()); }
    void OnTriggerStay2D(Collider2D other) { TryStartBattle(other.GetComponentInParent<playerMovement>()); }

    public bool IsPlayerInRange(playerMovement character, float extraDistance = 0f, bool checkSight = true)
    {
        if (character == null) return false;
        var encounterBox = GetComponent<BoxCollider2D>();
        var characterBox = character.GetComponent<Collider2D>();
        Vector2 origin = encounterBox != null && encounterBox.enabled ? encounterBox.bounds.center : transform.position;
        Vector2 target = characterBox != null ? characterBox.bounds.center : character.transform.position;
        float radius = Mathf.Max(.1f, interactionDistance) + Mathf.Max(0f, extraDistance);
        if (Mathf.Abs(origin.y - target.y) > maxEncounterHeightDifference ||
            (target - origin).sqrMagnitude > radius * radius) return false;
        if (checkSight)
            foreach (var hit in Physics2D.LinecastAll(origin, target))
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.CompareTag("Ground"))
                    return false;
        return true;
    }

    void TryStartBattle(playerMovement character)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;
        if (character == null || enemy == null || enteringBattle || Time.timeScale <= 0f ||
            GameControlsGuide.GameplayOverlayHidden || Time.unscaledTime < readyAt ||
            suppressedEncounter == gameObject.name) return;
        if (!IsPlayerInRange(character) || PlayerHealth.CurrentHealth <= 0 ||
            !Application.CanStreamedLevelBeLoaded("Battle Scene")) return;
        enteringBattle = true;
        suppressedEncounter = gameObject.name;
        GameSave.EnsureInstance().RememberBattleReturn(character.transform);
        GameSave.EnsureInstance().SaveGame(character.transform);
        List<EnemyData> squad = battleEncounter != null ? battleEncounter.CreateEnemySquad() : null;
        PendingEnemies = squad != null && squad.Count > 0 ? squad : new List<EnemyData> { enemy };
        PendingEncounterId = gameObject.name;
        SceneTransitionFader.Load("Battle Scene");
    }
}
