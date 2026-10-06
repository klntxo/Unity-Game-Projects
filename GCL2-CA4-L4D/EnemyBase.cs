using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [SerializeField] protected float maxHealth = 50f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float detectionRadius = 15f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackDamage = 5f;
    [SerializeField] protected float attackCooldown = 1.5f;

    [Header("Hit & Shove Settings")]
    [Tooltip("How many seconds of the death animation play as a hit flinch.")]
    [SerializeField] protected float hitReactionDuration = 0.35f;
    [Tooltip("Speed at which the enemy is knocked back when shoved.")]
    [SerializeField] protected float shoveKnockbackSpeed = 6f;
    [Tooltip("The state name of your death animation (for hit flinch).")]
    [SerializeField] protected string deathStateName = "Die";
    [Tooltip("The parameter trigger name in your Animator for actual death.")]
    [SerializeField] protected string deathTriggerName = "Die";

    [Header("Audio Clips")]
    [SerializeField] protected AudioClip ambientClip;
    [SerializeField] protected AudioClip attackClip;
    [SerializeField] protected AudioClip hitClip;
    [SerializeField] protected AudioClip deathClip;

    [Header("Ambient Audio")]
    [SerializeField] protected bool playAmbientSounds = true;
    [SerializeField] protected float ambientMinDelay = 4f;
    [SerializeField] protected float ambientMaxDelay = 8f;

    [Header("References")]
    [SerializeField] protected Transform playerTransform;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected AudioSource audioSource;

    protected enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Dead
    }

    protected EnemyState currentState = EnemyState.Idle;

    protected float lastAttackTime;
    private float nextAmbientTime;
    private Coroutine hitCoroutine;
    private bool isHitReacting = false;

    public static bool suppressEnemyAudio = false;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.loop = false;

        ScheduleNextAmbientSound();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning(
                    $"{gameObject.name}: No GameObject with the 'Player' tag was found."
                );
            }
        }
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead)
            return;

        if (playerTransform == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(transform.position, playerTransform.position);

        UpdateStateLogic(distanceToPlayer);
        UpdateAnimator();

        if (!suppressEnemyAudio)
        {
            UpdateAmbientAudio();
        }
    }

    protected virtual void UpdateStateLogic(float distanceToPlayer)
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;

                if (distanceToPlayer <= detectionRadius)
                {
                    OnDetectedPlayer();
                }
                break;

            case EnemyState.Chasing:
                if (!agent.enabled || !agent.isOnNavMesh)
                    return;

                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);

                if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.Attacking;
                    agent.isStopped = true;
                }
                else if (distanceToPlayer > detectionRadius * 1.5f)
                {
                    currentState = EnemyState.Idle;
                    agent.isStopped = true;
                }
                break;

            case EnemyState.Attacking:
                if (!agent.enabled || !agent.isOnNavMesh)
                    return;

                agent.isStopped = true;
                FacePlayer();

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }

                if (distanceToPlayer > attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                break;
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (currentState == EnemyState.Dead)
            return;

        currentHealth -= amount;
        PlayOneShotSound(hitClip);

        if (currentState == EnemyState.Idle)
        {
            OnDetectedPlayer();
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }
            hitCoroutine = StartCoroutine(HitReactionRoutine());
        }
    }

    /// <summary>
    /// Shoves the enemy backward, interrupts active attacks, deals damage, and triggers hit flinch.
    /// </summary>
    public virtual void Shove(Vector3 shoveDirection, float shoveDamage)
    {
        if (currentState == EnemyState.Dead)
            return;

        // Deal damage first (will call Die() automatically if health drops to 0)
        TakeDamage(shoveDamage);

        if (currentState == EnemyState.Dead)
            return;

        // Interrupt active attack state immediately
        if (currentState == EnemyState.Attacking)
        {
            currentState = EnemyState.Chasing;
        }

        // Cancel any active hit reaction routine to start the shove reaction
        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }

        hitCoroutine = StartCoroutine(ShoveReactionRoutine(shoveDirection.normalized));
    }

    private IEnumerator HitReactionRoutine()
    {
        if (animator == null) yield break;

        isHitReacting = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator.SetFloat("Speed", 0f);
        animator.Play(deathStateName, 0, 0f);

        yield return new WaitForSeconds(hitReactionDuration);

        if (currentState != EnemyState.Dead)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
            isHitReacting = false;
            UpdateAnimator();
        }
    }

    private IEnumerator ShoveReactionRoutine(Vector3 shoveDirection)
    {
        if (animator == null) yield break;

        isHitReacting = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator.SetFloat("Speed", 0f);
        animator.Play(deathStateName, 0, 0f);

        float elapsed = 0f;
        while (elapsed < hitReactionDuration)
        {
            if (currentState == EnemyState.Dead) yield break;

            // Move along NavMesh during flinch duration so enemy doesn't pass through walls
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(shoveDirection * (shoveKnockbackSpeed * Time.deltaTime));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState != EnemyState.Dead)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
            isHitReacting = false;
            UpdateAnimator();
        }
    }

    protected virtual void OnDetectedPlayer()
    {
        currentState = EnemyState.Chasing;
        PlayOneShotSound(ambientClip);
        ScheduleNextAmbientSound();
    }

    protected virtual void PerformAttack()
    {
        PlayOneShotSound(attackClip);

        if (playerTransform == null)
            return;

        IDamageable playerDamageable =
            playerTransform.GetComponent<IDamageable>();

        if (playerDamageable != null)
        {
            playerDamageable.TakeDamage(attackDamage);
        }
    }

    protected virtual void UpdateAnimator()
    {
        if (animator == null || isHitReacting || currentState == EnemyState.Dead)
            return;

        float speed = 0f;

        if (agent != null && agent.enabled)
        {
            speed = agent.velocity.magnitude;
        }

        animator.SetFloat("Speed", speed);
    }

    protected virtual void UpdateAmbientAudio()
    {
        if (!playAmbientSounds || currentState != EnemyState.Idle)
            return;

        if (ambientClip == null || audioSource == null)
            return;

        if (Time.time < nextAmbientTime)
            return;

        if (playerTransform != null)
        {
            float distance =
                Vector3.Distance(transform.position, playerTransform.position);

            if (distance > detectionRadius)
            {
                ScheduleNextAmbientSound();
                return;
            }
        }

        PlayOneShotSound(ambientClip);
        ScheduleNextAmbientSound();
    }

    protected void FacePlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }

    protected void ScheduleNextAmbientSound()
    {
        nextAmbientTime =
            Time.time + Random.Range(ambientMinDelay, ambientMaxDelay);
    }

    protected virtual void Die()
    {
        if (currentState == EnemyState.Dead)
            return;

        currentState = EnemyState.Dead;
        isHitReacting = false;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        PlayOneShotSound(deathClip);

        if (animator != null)
        {
            animator.SetTrigger(deathTriggerName);
            animator.Play(deathStateName, 0, 0f);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 5f);
    }

    protected void PlayOneShotSound(AudioClip clip)
    {
        if (suppressEnemyAudio) return;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}