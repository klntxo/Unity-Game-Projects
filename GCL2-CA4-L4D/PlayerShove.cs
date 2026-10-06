using UnityEngine;

public class PlayerShove : MonoBehaviour
{
    [Header("Input & Cooldown")]
    [SerializeField] private KeyCode shoveKey = KeyCode.Mouse1;
    [SerializeField] private float shoveCooldown = 1.0f;

    [Header("Shove Stats")]
    [SerializeField] private float shoveRange = 2.5f;
    [SerializeField] private float shoveRadius = 1.0f; // Broad check so close targets aren't missed
    [SerializeField] private float shoveDamage = 15f;

    [Header("Targeting Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("Audio & Animation (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shoveSound;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string shoveAnimationTrigger = "Shove";

    private float nextShoveTime;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(shoveKey) && Time.time >= nextShoveTime)
        {
            PerformShove();
        }
    }

    private void PerformShove()
    {
        nextShoveTime = Time.time + shoveCooldown;

        // Play animation feedback
        if (playerAnimator != null && !string.IsNullOrEmpty(shoveAnimationTrigger))
        {
            playerAnimator.SetTrigger(shoveAnimationTrigger);
        }

        // Play sound feedback
        if (audioSource != null && shoveSound != null)
        {
            audioSource.PlayOneShot(shoveSound);
        }

        // Cast a sphere forward from camera to catch enemies in front of the player
        RaycastHit[] hits = Physics.SphereCastAll(
            cameraTransform.position,
            shoveRadius,
            cameraTransform.forward,
            shoveRange,
            enemyLayer
        );

        foreach (RaycastHit hit in hits)
        {
            EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                // Push direction is directly away from player camera
                Vector3 shoveDir = cameraTransform.forward;
                shoveDir.y = 0; // Keep push horizontal on NavMesh

                enemy.Shove(shoveDir, shoveDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visual debug helper in Scene view
        Transform origin = cameraTransform != null ? cameraTransform : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position + origin.forward * shoveRange, shoveRadius);
    }
}