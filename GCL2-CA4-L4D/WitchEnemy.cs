using UnityEngine;

public class WitchEnemy : EnemyBase
{
    [Header("Witch Specific Settings")]
    [SerializeField] private float startleRadius = 4f;
    [SerializeField] private AudioSource cryingAudioSource;
    [SerializeField] private AudioClip startleScreamClip;

    private bool isStartled = false;
    private PlayerMovement playerMovement;

    protected override void Start()
    {
        // Witch stats
        maxHealth = 100f;
        attackDamage = 25f;
        attackCooldown = 2f;
        detectionRadius = 25f;
        attackRange = 2f;

        playAmbientSounds = false; // Witch cries instead of random groans

        base.Start();

        // Find the player's movement script to track their exact speed
        if (playerTransform != null)
        {
            playerMovement = playerTransform.GetComponent<PlayerMovement>();
        }

        // Initialize crying audio
        if (cryingAudioSource != null && !cryingAudioSource.isPlaying)
        {
            cryingAudioSource.loop = true;
            cryingAudioSource.Play();
        }
    }

    protected override void Update()
    {
        base.Update();

        // Stop updating crying volume during player death scream
        if (suppressEnemyAudio) return;

        // Handle proximity-based crying audio if not startled
        if (!isStartled && currentState == EnemyState.Idle && playerTransform != null && cryingAudioSource != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= detectionRadius)
            {
                float factor = 1.0f - Mathf.Clamp01(dist / detectionRadius);
                cryingAudioSource.volume = Mathf.Lerp(0.2f, 1.0f, factor);
                cryingAudioSource.pitch = Mathf.Lerp(0.9f, 1.2f, factor);
            }
            else
            {
                cryingAudioSource.volume = 0f;
            }
        }

        if (isStartled && currentState == EnemyState.Chasing)
        {
            UpdateWitchSpeed();
        }
    }

    protected override void UpdateStateLogic(float distanceToPlayer)
    {
        // Witch remains idle and crying until startled
        if (!isStartled)
        {
            if (distanceToPlayer <= startleRadius)
            {
                StartleWitch();
            }
            return;
        }

        // Once startled, use normal EnemyBase chase/attack behaviour
        base.UpdateStateLogic(distanceToPlayer);
    }

    private void StartleWitch()
    {
        if (isStartled) return;

        isStartled = true;
        currentState = EnemyState.Chasing;

        // Stop crying
        if (cryingAudioSource != null)
        {
            cryingAudioSource.Stop();
            cryingAudioSource.volume = 0f;
        }

        // Play scream
        PlayOneShotSound(startleScreamClip);

        // Play startle animation
        if (animator != null)
        {
            animator.SetTrigger("Startled");
        }

        // Immediately apply chase speed
        UpdateWitchSpeed();
    }

    private void UpdateWitchSpeed()
    {
        if (agent == null || playerMovement == null) return;

        // Witch always moves 1.1x the player's CURRENT speed
        agent.speed = playerMovement.CurrentMoveSpeed * 1.1f;
    }

    protected override void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("DualHandAttack");
        }

        // Attack sound + player damage
        base.PerformAttack();
    }

    public override void TakeDamage(float amount)
    {
        // Shooting the Witch wakes her immediately
        if (!isStartled)
        {
            StartleWitch();
        }

        base.TakeDamage(amount);
    }

    protected override void Die()
    {
        // Stop crying when Witch dies
        if (cryingAudioSource != null)
        {
            cryingAudioSource.Stop();
        }

        base.Die();
    }
}