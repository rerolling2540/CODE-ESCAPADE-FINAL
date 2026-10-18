using UnityEngine;

// Visual-only motion, applied after the sprite animator. The feet remain anchored
// to the player capsule while the artwork settles between movement poses.
public class ArcherAnimationPolish : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private Vector3 restingScale;
    private Quaternion restingRotation;
    private Vector3 restingPosition;
    private bool floating;
    private float landingTime, landingStrength, takeoffTime, previousVerticalSpeed;
    private float airborneTime, idleTime;
    private bool wasGrounded, initialized;

    public void Configure(Animator visualAnimator, Rigidbody2D playerBody, SpriteRenderer renderer, bool hover = false)
    {
        animator = visualAnimator;
        body = playerBody;
        sprite = renderer;
        restingScale = transform.localScale;
        restingRotation = transform.localRotation;
        restingPosition = transform.localPosition;
        floating = hover;
    }

    private void LateUpdate()
    {
        if (animator == null || body == null || sprite == null || Time.timeScale <= 0f) return;
        float dt = Time.deltaTime;
        bool grounded = animator.GetBool("Grounded");
        bool dashing = animator.GetBool("Dashing");
        bool dialogue = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        float vertical = body.linearVelocity.y;
        float speed = Mathf.Abs(body.linearVelocity.x);

        // Detect real impacts and fresh upward impulses, including the air jump.
        // Ignore initial spawn and the brief grounded grace across tile seams.
        if (initialized && grounded && !wasGrounded && airborneTime > 0.08f && previousVerticalSpeed < -1f)
        {
            landingTime = 0.18f;
            landingStrength = Mathf.Clamp01(-previousVerticalSpeed / 16f);
        }
        if (initialized && !grounded && vertical > 1f && vertical - previousVerticalSpeed > 2f)
        {
            takeoffTime = 0.12f;
            landingTime = 0f;
        }
        airborneTime = grounded ? 0f : airborneTime + dt;
        wasGrounded = grounded;
        previousVerticalSpeed = vertical;
        initialized = true;

        landingTime = Mathf.Max(0f, landingTime - dt);
        takeoffTime = Mathf.Max(0f, takeoffTime - dt);
        Vector2 shape = Vector2.one;
        float lean = 0f;
        float direction = sprite.flipX ? -1f : 1f;

        if (!dialogue)
        {
            if (dashing)
            {
                // A low, forward silhouette distinguishes the burst from running.
                shape = new Vector2(1.06f, 0.93f);
                lean = -direction * 7f;
            }
            else if (!grounded)
            {
                float rise = Mathf.Clamp01(vertical / 15f);
                float fall = Mathf.Clamp01(-vertical / 15f);
                float launch = takeoffTime / 0.12f;
                shape = new Vector2(1f - 0.035f * rise - 0.025f * launch,
                    1f + 0.04f * rise + 0.035f * launch - 0.025f * fall);
                lean = -direction * (rise * 2f + fall * 3f);
            }
            else if (landingTime > 0f)
            {
                // Ease out of a short impact squash without holding movement input.
                float impact = landingTime / 0.18f;
                impact = impact * impact * landingStrength;
                shape = new Vector2(1f + 0.07f * impact, 1f - 0.09f * impact);
            }
            else if (speed > 0.2f)
            {
                lean = -direction * Mathf.Min(2.5f, speed * 0.3f);
            }
            else
            {
                idleTime += dt;
                shape.y = 1f + Mathf.Sin(idleTime * 2.5f) * 0.008f;
            }
        }

        float blend = 1f - Mathf.Exp(-24f * dt);
        var targetScale = new Vector3(restingScale.x * shape.x, restingScale.y * shape.y, restingScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blend);
        transform.localRotation = Quaternion.Slerp(transform.localRotation,
            restingRotation * Quaternion.Euler(0f, 0f, lean * (floating ? 0.65f : 1f)), blend);
        if (floating)
        {
            // Hover is purely visual: jumping and ground collision still use the player root.
            float lift = dialogue ? 0.06f : 0.06f + Mathf.Sin(Time.time * 3f) * (grounded ? 0.025f : 0.01f);
            transform.localPosition = Vector3.Lerp(transform.localPosition,
                restingPosition + Vector3.up * lift, blend);
        }
    }

    private void OnDisable()
    {
        if (animator == null) return;
        transform.localScale = restingScale;
        transform.localRotation = restingRotation;
        transform.localPosition = restingPosition;
        initialized = false;
        landingTime = takeoffTime = airborneTime = idleTime = previousVerticalSpeed = 0f;
    }
}
