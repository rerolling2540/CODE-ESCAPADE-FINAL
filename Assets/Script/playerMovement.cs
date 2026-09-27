using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public int maxJumps = 2;

    private int jumpCount = 0;
    private bool isGrounded = false;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    private float movement;
    private bool facingRight = true;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // MOVEMENT
        movement = Input.GetAxisRaw("Horizontal");

        // FLIP LEFT
        if (movement < 0 && facingRight)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );

            facingRight = false;
        }

        // FLIP RIGHT
        else if (movement > 0 && !facingRight)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );

            facingRight = true;
        }

        // RUNNING ANIMATION
        if (Mathf.Abs(movement) > 0.1f)
            animator.SetFloat("Running", 1f);
        else
            animator.SetFloat("Running", 0f);

        // DOUBLE JUMP
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            && !isDashing)
        {
            if (jumpCount < maxJumps)
            {
                Jump();
            }
        }

        // DASH
        if (Input.GetKeyDown(KeyCode.LeftShift)
            && !isDashing
            && dashCooldownTimer <= 0f)
        {
            Dash();
        }

        // DASH TIMER
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
                isDashing = false;
        }

        // DASH COOLDOWN
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // JUMP / FALL ANIMATION
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                animator.SetBool("Jumping", true);
                animator.SetBool("Falling", false);
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                animator.SetBool("Jumping", false);
                animator.SetBool("Falling", true);
            }
        }
        else
        {
            animator.SetBool("Jumping", false);
            animator.SetBool("Falling", false);
        }
    }

    void FixedUpdate()
    {
        // DASH MOVEMENT
        if (isDashing)
        {
            float dashDirection = facingRight ? 1f : -1f;

            rb.linearVelocity = new Vector2(
                dashDirection * dashSpeed,
                rb.linearVelocity.y
            );

            return;
        }

        // NORMAL MOVEMENT
        rb.linearVelocity = new Vector2(
            movement * moveSpeed,
            rb.linearVelocity.y
        );
    }

    void Jump()
    {
        // Reset vertical velocity para consistent ang jump
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            0f
        );

        // Jump
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        jumpCount++;

        isGrounded = false;

        animator.SetBool("Jumping", true);
    }

    void Dash()
    {
        isDashing = true;

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        float dashDirection = facingRight ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            dashDirection * dashSpeed,
            rb.linearVelocity.y
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;

                    // Reset jumps kapag nasa ground
                    jumpCount = 0;

                    break;
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;

                    // Reset jumps kapag nasa ground
                    jumpCount = 0;

                    return;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
    }
}