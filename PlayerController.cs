using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public Text healthText; // Bind UI components

    public float runSpeed = 5f;
    public float jumpForce = 10f;

    public LayerMask groundLayer; // Ground floor
    public Transform groundCheck; // Ground inspection points
    public float groundCheckRadius = 0.2f; // Ground detection range
    public GameObject attackHitbox; // Attack colliders
    public float attcakTiming = 0.15f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D normalCollider;
    private Collider2D slideCollider;
    private bool isGrounded;
    private bool isSliding;
    private bool isJumping;
    private bool isFalling;
    private bool isAttacking;
    private bool isKnockedBack;


    public int playerHealth = 3; // Player health
    public float knockbackForce = 5f; // Damage knockback strength
    public float invincibleTime = 0.2f; // Invincible time
    public float knockBackTiming = 0.5f;
    private bool isInvincible;

    public GameObject gameOverPanel;
    void Start()
    {
        UpdateHealthUI(); // Make sure the UI shows the correct amount of health when the game starts
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        normalCollider = GetComponent<Collider2D>();
        slideCollider = transform.Find("SlideCollider").GetComponent<Collider2D>();

        slideCollider.enabled = false;
        attackHitbox.SetActive(false); // Initial closure of the attack zone

    }

    void Update()
    {

        if (!isKnockedBack)
        {
            rb.velocity = new Vector2(runSpeed, rb.velocity.y);
        }
        // Detect out-of-bounds
        if (transform.position.y < -8)
        {
            Respawn();
        }

        // Physically detects whether it is on the ground
        isGrounded = CheckIfGrounded();

        // Handle jump inputs
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isSliding)
        {
            Jump();
        }

        // Handle slide inputs
        if (Input.GetKey(KeyCode.S) && isGrounded) // Keep holding down the S key
        {
            StartSlide();
        }
        else if (Input.GetKeyUp(KeyCode.S)) // Resumes when the S key is released
        {
            StopSlide();
        }

        // Handle attack inputs
        if (Input.GetMouseButtonDown(0)) // Press the left mouse button to attack
        {
            Attack();
        }

        // Updated jump animations
        UpdateJumpAnimation();
    }
    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + playerHealth;
        }
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true;
        isFalling = false;
        animator.SetBool("IsJumping", true);
        animator.SetBool("IsFalling", false);
    }

    void StartSlide()
    {
        if (!isSliding)
        {
            isSliding = true;
            animator.SetBool("IsSliding", true);

            // Toggle colliders
            normalCollider.enabled = false;
            slideCollider.enabled = true;
        }
    }

    void StopSlide()
    {
        if (isSliding)
        {
            isSliding = false;
            animator.SetBool("IsSliding", false);

            // Restore the collider
            normalCollider.enabled = true;
            slideCollider.enabled = false;
        }
    }

    void Attack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            animator.SetTrigger("Attack"); // Play the attack animation
            attackHitbox.SetActive(true); // Enables Attack Collider

            // After a certain period of time, the attacking collider is turned off
            StartCoroutine(ResetAttack());
        }
    }

    IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(attcakTiming); // Assuming the attack animation lasts 0.5 seconds
        attackHitbox.SetActive(false);
        isAttacking = false;

        // Restore the animation based on the current state
        if (isGrounded)
        {
            animator.SetBool("IsRunning", true);
        }
        else
        {
            if (rb.velocity.y > 0)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);
            }
            else
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", true);
            }
        }
    }

    void UpdateJumpAnimation()
    {
        if (!isGrounded)
        {
            if (rb.velocity.y > 0) // Jump up
            {
                isJumping = true;
                isFalling = false;
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);
            }
            else if (rb.velocity.y < 0) // whereabouts
            {
                isJumping = false;
                isFalling = true;
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", true);
            }
        }
        else // The character touches the ground
        {
            if (isFalling) // Toggle only when in the Falling state
            {
                isJumping = false;
                isFalling = false;
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", false);
            }
        }
    }

    bool CheckIfGrounded()
    {
        // 在地面检测点发射一个小范围的射线来检测地面
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !isInvincible)
        {
            TakeDamage(collision.gameObject.transform);
        }
    }
    void TakeDamage(Transform enemy)
    {
        if (isInvincible) return; // Prevent repeated injuries

        playerHealth--; // Health decreased
        playerHealth = Mathf.Max(playerHealth, 0);
        animator.SetTrigger("Hurt"); // Play the injury animation
        UpdateHealthUI(); // Update the UI
        if (playerHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(BecomeInvincible());

        float knockbackDirection = transform.position.x > enemy.position.x ? 1 : -1;
        rb.velocity = new Vector2(knockbackDirection * knockbackForce, 3f);
        isKnockedBack = true;
        StartCoroutine(ResetKnockback());


        // 取消滑铲状态，防止玩家滑铲时受伤出现异常
        if (isSliding)
        {
            StopSlide();
        }

        // Briefly stop running
        StartCoroutine(StopRunningTemporarily());
    }


    void Die()
    {
        animator.SetTrigger("Die");
        StartCoroutine(GameOverPanel());
        this.enabled = false; // 禁用 PlayerController
        rb.velocity = Vector2.zero; // Clearing speed
    }

    IEnumerator BecomeInvincible()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }
    IEnumerator StopRunningTemporarily()
    {
        float originalSpeed = runSpeed;
        runSpeed = 0; // Stop moving forward

        yield return new WaitForSeconds(1f); // Pauses for 0.3 seconds after taking a hit

        runSpeed = originalSpeed; // Recovery speed
    }
    IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(knockBackTiming); // Knockback duration击退持续时间
        isKnockedBack = false;
    }
    void Respawn()
    {
        playerHealth--; // Health decreased
        playerHealth = Mathf.Max(playerHealth, 0);
        UpdateHealthUI(); // Update the UI
        if (playerHealth <= 0)
        {
            Die();
            return;
        }

        // The reset to X position remains unchanged, but the Y axis is set to 8
        transform.position = new Vector3(transform.position.x, 8f, transform.position.z);
    }
     IEnumerator GameOverPanel()
    {
        yield return new WaitForSeconds(1f);
        gameOverPanel.SetActive(true);
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("StartScene");
    }
}
