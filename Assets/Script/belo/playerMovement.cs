using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D rb;

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

    private float movement;
    private bool isGrounded;
    private int airJumpsRemaining = 1;
    private bool facingRight = true;
    private SpriteRenderer spriteRenderer;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private float lastGroundedTime = float.NegativeInfinity;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashDirection;
    private bool runningPose;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer != null) facingRight = !spriteRenderer.flipX;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

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
    }

    void FixedUpdate()
    {
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
        isGrounded = groundContacts.Count > 0;
        if (isGrounded)
        {
            airJumpsRemaining = 1;
            lastGroundedTime = Time.time;
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
        groundContacts.Clear();
        isGrounded = false;
        lastGroundedTime = float.NegativeInfinity;
        isDashing = false;
        movement = 0f;
        runningPose = false;
        airJumpsRemaining = 1;
    }
}
