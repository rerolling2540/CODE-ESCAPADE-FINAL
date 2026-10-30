using UnityEngine;

/// <summary>A one-shot lightning penalty for incorrect switch and portal answers.</summary>
[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
public sealed class QuestionLightningStrike : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField, Min(0)] private int firstFrame = 3;
    [SerializeField, Min(1)] private int damage = 5;
    [SerializeField] private AudioClip strikeSound;
    private playerMovement target;
    private Collider2D targetBody;
    private SpriteRenderer visual;
    private float elapsed;
    private bool struck;

    public static QuestionLightningStrike Spawn()
    {
        var prefab = Resources.Load<GameObject>("QuestionPenalty/Lightning Strike");
        if (prefab == null)
        {
            Debug.LogError("Question lightning penalty prefab is missing.");
            PlayerHealth.Damage(5, PlayerDamageCause.Lightning);
            return null;
        }
        var strike = Instantiate(prefab).GetComponent<QuestionLightningStrike>();
        strike.Strike(FindFirstObjectByType<playerMovement>());
        return strike;
    }

    public void Strike(playerMovement player)
    {
        if (struck) return;
        struck = true;
        target = player;
        visual = GetComponent<SpriteRenderer>();
        if (target != null)
        {
            targetBody = target.GetComponent<Collider2D>();
            var playerVisual = target.GetComponentInChildren<SpriteRenderer>();
            if (playerVisual != null)
            {
                visual.sortingLayerID = playerVisual.sortingLayerID;
                visual.sortingOrder = playerVisual.sortingOrder + 20;
            }
            FollowTarget();
        }
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.Clamp(firstFrame, 0, frames.Length - 1)];
        // The penalty must still apply if a recent trap hit granted contact immunity.
        if (PlayerHealth.CurrentHealth > 0 && (target == null || !target.TryTakeDamage(damage, PlayerDamageCause.Lightning)))
            PlayerHealth.Damage(damage, PlayerDamageCause.Lightning);
        var audio = GetComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;
        audio.volume = 0.55f;
        GameAudio.Route(audio, false);
        if (strikeSound != null) audio.PlayOneShot(strikeSound);
    }

    private void Update()
    {
        if (!struck) return;
        elapsed += Time.deltaTime;
        int frame = firstFrame + Mathf.FloorToInt(elapsed * framesPerSecond);
        if (frames == null || frame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        visual.sprite = frames[frame];
    }

    private void LateUpdate() => FollowTarget();

    private void FollowTarget()
    {
        if (target == null) return;
        Vector3 position = target.transform.position;
        if (targetBody != null)
        {
            position.x = targetBody.bounds.center.x;
            position.y = targetBody.bounds.min.y;
        }
        transform.position = position;
    }
}
