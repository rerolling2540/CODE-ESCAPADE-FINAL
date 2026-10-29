using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D rb;
    [Tooltip("Use the archer artwork even when this prefab is previewed without character selection.")]
    public bool archerAppearance;
    [Tooltip("Use the mage artwork when this prefab is placed directly in a scene.")]
    public bool mageAppearance;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 15f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 0.5f;
    public AnimationClip dashAnimation;

    [Header("Animation")]
    [Min(0.1f)] public float runAnimationSpeed = 1.35f;
    [Min(0f)] public float groundAnimationGrace = 0.06f;

    [Header("Damage Reaction")]
    public AnimationClip hurtAnimation;
    [Min(0.1f)] public float damageCooldown = 0.8f;
    public bool IsHurt => Time.time < hurtUntil;

    private float movement;
    private bool isGrounded;
    private int airJumpsRemaining = 1;
    private bool facingRight = true;
    private SpriteRenderer spriteRenderer;
    private CharacterSmokeEffects smokeEffects;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private float lastGroundedTime = float.NegativeInfinity;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashDirection;
    private bool runningPose;
    private bool hasBeenAirborne;
    private float hurtUntil = float.NegativeInfinity;
    private float nextDamageTime = float.NegativeInfinity;
    private bool hasHurtState;
    private Sprite[] damageFrames;
    private float damageAnimationStart;
    private Color restingColor = Color.white;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (mageAppearance || (!archerAppearance && SelectedHero.IsMage))
            spriteRenderer = SelectedHero.ApplyMage(this, spriteRenderer);
        else if (archerAppearance || SelectedHero.IsArcher)
            spriteRenderer = SelectedHero.ApplyArcher(this, spriteRenderer);
        if (spriteRenderer != null) facingRight = !spriteRenderer.flipX;
        if (spriteRenderer != null) restingColor = spriteRenderer.color;
        if(archerAppearance||mageAppearance||SelectedHero.IsArcher||SelectedHero.IsMage)
        {
            var bank=Resources.Load<CharacterHurtFrames>("PlayerDeath/Hurt");
            int character=mageAppearance||SelectedHero.IsMage?2:1;
            if(bank!=null&&spriteRenderer!=null)damageFrames=bank.Frames(character,spriteRenderer.sprite.pixelsPerUnit);
        }
        hasHurtState = animator != null && animator.HasState(0, Animator.StringToHash("Hurt"));

        smokeEffects = GetComponent<CharacterSmokeEffects>();
        if (smokeEffects == null) smokeEffects = gameObject.AddComponent<CharacterSmokeEffects>();
        smokeEffects.Initialize(spriteRenderer, GetComponent<CapsuleCollider2D>());
        PlayerDeathFlow.EnsureInstance();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (hasHurtState) animator.SetBool("Hurt", IsHurt);
        if (IsHurt)
        {
            movement = 0f;
            UpdateAnimation();
            return;
        }
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            movement = 0f;
            isDashing = false;
            UpdateAnimation();
            return;
        }

        if (!isGrounded && rb != null && rb.linearVelocity.y < -0.5f)
            hasBeenAirborne = true;

        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }

        movement = Input.GetAxisRaw("Horizontal");
        if (!isDashing && Mathf.Abs(movement) > 0.1f)
        {
            facingRight = movement > 0f;
            // Keep the collider and attached objects stationary when turning.
            if (spriteRenderer != null) spriteRenderer.flipX = !facingRight;
        }

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            && (isGrounded || airJumpsRemaining > 0))
            Jump();

        else if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && dashCooldownTimer <= 0f)
            Dash();

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (Time.timeScale <= 0f || animator == null || rb == null) return;

        // Briefly bridge tile seams without delaying a deliberate jump or a fall.
        bool groundedPose = isGrounded ||
            (Time.time - lastGroundedTime <= groundAnimationGrace &&
             Mathf.Abs(rb.linearVelocity.y) < 0.5f);
        float speed = Mathf.Abs(rb.linearVelocity.x);
        // Hysteresis keeps tiny collision-speed changes from toggling idle/run.
        runningPose = Mathf.Abs(movement) > 0.1f && speed > (runningPose ? 0.05f : 0.2f);
        animator.SetFloat("Running", runningPose ? 1f : 0f);
        animator.SetFloat("RunAnimationSpeed", runAnimationSpeed *
            Mathf.Clamp(speed / Mathf.Max(moveSpeed, 0.01f), 0.5f, 1.5f),
            0.08f, Time.deltaTime);
        animator.SetBool("Grounded", groundedPose);
        animator.SetBool("Dashing", isDashing);
        // Rising includes the apex so the pose does not flicker around zero velocity.
        animator.SetBool("Jumping", !groundedPose && !isDashing && rb.linearVelocity.y >= -0.1f);
        animator.SetBool("Falling", !groundedPose && !isDashing && rb.linearVelocity.y < -0.1f);
        if (smokeEffects != null)
            smokeEffects.UpdateRunning(runningPose && !isDashing, isGrounded, facingRight);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        if (IsHurt)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float horizontalSpeed = movement * moveSpeed;
        if (isDashing)
        {
            // Keep the initial burst, then ease into normal movement over the last quarter.
            float recovery = 1f - Mathf.Clamp01(dashTimer / Mathf.Max(dashDuration * 0.25f, 0.01f));
            float exitSpeed = Mathf.Max(0f, movement * dashDirection) * moveSpeed;
            horizontalSpeed = dashDirection * Mathf.Lerp(dashSpeed, exitSpeed,
                Mathf.SmoothStep(0f, 1f, recovery));
        }
        rb.linearVelocity = new Vector2(
            horizontalSpeed,
            rb.linearVelocity.y);
    }

    public bool TryTakeDamage(int amount)
    {
        if (amount <= 0 || Time.timeScale <= 0f || Time.time < nextDamageTime ||
            PlayerHealth.CurrentHealth <= 0 || !isActiveAndEnabled) return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return false;

        PlayerHealth.Damage(amount);
        damageAnimationStart=Time.time;
        float duration = hurtAnimation != null ? Mathf.Max(0.1f, hurtAnimation.length) : 0.4f;
        if(damageFrames!=null)duration=damageFrames.Length/10f;
        hurtUntil = Time.time + duration;
        nextDamageTime = Time.time + Mathf.Max(damageCooldown, duration);
        movement = 0f;
        isDashing = false;
        dashTimer = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        UpdateAnimation();
        if (hasHurtState)
        {
            animator.SetBool("Hurt", true);
            animator.Play("Hurt", 0, 0f);
        }
        return true;
    }

    void LateUpdate()
    {
        if (spriteRenderer == null) return;
        if(!PlayerDeathFlow.IsActive&&PlayerHealth.CurrentHealth>0&&IsHurt&&damageFrames!=null&&damageFrames.Length>0)
            spriteRenderer.sprite=damageFrames[Mathf.Min(damageFrames.Length-1,Mathf.FloorToInt((Time.time-damageAnimationStart)*10f))];
        // Also gives the archer and mage a visible hit response using their own artwork.
        bool flash = Time.time < nextDamageTime && Mathf.FloorToInt((nextDamageTime - Time.time) * 12f) % 2 == 0;
        spriteRenderer.color = flash ? Color.Lerp(restingColor, new Color(1f, 0.2f, 0.15f), 0.75f) : restingColor;
    }

    void Jump()
    {
        // Jump cancels the dash immediately, without restoring spent air jumps.
        isDashing = false;
        dashTimer = 0f;
        animator.SetBool("Dashing", false);
        if (!isGrounded) airJumpsRemaining--;
        isGrounded = false;
        groundContacts.Clear();
        lastGroundedTime = float.NegativeInfinity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        hasBeenAirborne = true;
        if (smokeEffects != null) smokeEffects.PlayJump(facingRight);
        animator.SetBool("Grounded", false);
        animator.SetBool("Falling", false);
        animator.SetBool("Jumping", true);
        // Restart only for a new jump, including the second jump.
        animator.Play("Jumping", 0, 0f);
    }

    void Dash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = facingRight ? 1f : -1f;
        if (smokeEffects != null) smokeEffects.PlayDash(facingRight);
        animator.SetBool("Dashing", true);
        animator.SetBool("Jumping", false);
        animator.SetBool("Falling", false);
        float clipLength = dashAnimation != null ? dashAnimation.length : 0.35f;
        animator.SetFloat("DashAnimationSpeed", clipLength / Mathf.Max(dashDuration, 0.01f));
        animator.Play("Dash", 0, 0f);
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
    }

    void UpdateGroundContact(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground")) return;
        bool supporting = false;
        if (rb.linearVelocity.y <= 0.5f)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    supporting = true;
                    break;
                }
            }
        }
        if (supporting) groundContacts.Add(collision.collider);
        else groundContacts.Remove(collision.collider);
        bool wasGrounded = isGrounded;
        isGrounded = groundContacts.Count > 0;
        if (isGrounded)
        {
            airJumpsRemaining = 1;
            lastGroundedTime = Time.time;
            if (!wasGrounded && hasBeenAirborne)
            {
                if (smokeEffects != null) smokeEffects.PlayLanding(facingRight);
                hasBeenAirborne = false;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision) { UpdateGroundContact(collision); }
    void OnCollisionStay2D(Collision2D collision) { UpdateGroundContact(collision); }

    void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
        isGrounded = groundContacts.Count > 0;
    }

    void OnDisable()
    {
        if (hasHurtState && animator != null) animator.SetBool("Hurt", false);
        if (spriteRenderer != null) spriteRenderer.color = restingColor;
        hurtUntil = nextDamageTime = float.NegativeInfinity;
        groundContacts.Clear();
        isGrounded = false;
        lastGroundedTime = float.NegativeInfinity;
        isDashing = false;
        movement = 0f;
        runningPose = false;
        airJumpsRemaining = 1;
        hasBeenAirborne = false;
    }
}
