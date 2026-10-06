using UnityEngine;

public class Ladder : MonoBehaviour
{
    [Header("Climb Settings")]
    public float climbSpeed = 2.5f;

    private bool isClimbing = false;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = other.GetComponent<Collider2D>();
        Animator anim = other.GetComponent<Animator>();

        if (rb == null || playerCollider == null) return;

        float verticalInput = Input.GetAxisRaw("Vertical");

        // Activate climbing when Up or Down is pressed
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            isClimbing = true;
        }

        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalInput * climbSpeed);

            // Trigger climbing animation and freeze frame when stationary on ladder
            if (anim != null)
            {
                anim.SetBool("isClimbing", true);
                anim.speed = (Mathf.Abs(verticalInput) > 0.01f) ? 1f : 0f;
            }

            // Disable collider when pressing DOWN to pass through platforms beneath
            if (verticalInput < 0)
            {
                playerCollider.isTrigger = true;
            }
            else
            {
                playerCollider.isTrigger = false;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isClimbing = false;

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            Collider2D playerCollider = other.GetComponent<Collider2D>();
            Animator anim = other.GetComponent<Animator>();

            if (rb != null)
            {
                rb.gravityScale = 1f;
            }

            if (playerCollider != null)
            {
                playerCollider.isTrigger = false;
            }

            // Reset Animator to normal state & playback speed when leaving ladder
            if (anim != null)
            {
                anim.SetBool("isClimbing", false);
                anim.speed = 1f;
            }
        }
    }
}