using UnityEngine;

public class BasicEnemy : EnemyBase
{
    protected override void Start()
    {
        // Basic enemy stats
        maxHealth = 30f;
        attackDamage = 5f;
        attackCooldown = 1.2f;
        detectionRadius = 12f;
        attackRange = 1.8f;

        // Basic zombie occasionally groans while idle.
        playAmbientSounds = true;
        ambientMinDelay = 4f;
        ambientMaxDelay = 8f;

        base.Start();
    }

    protected override void PerformAttack()
    {
        // Attack animation
        if (animator != null)
        {
            animator.SetTrigger("MeleeAttack");
        }

        // Attack sound + player damage
        base.PerformAttack();
    }
}