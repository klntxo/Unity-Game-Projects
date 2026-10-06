using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health;

    [Header("Health Bar")]
    public Image frontHealthBar;
    public Image backHealthBar;
    public TMP_Text healthText;

    [Header("Lives / Checkpoint")]
    public int maxLives = 3;
    public int currentLives = 3;
    public LivesText livesUI;
    public PlayerRespawn playerRespawn;

    [Header("Chip Effect")]
    public float chipSpeed = 2f;

    [Header("Game Over Settings")]
    public GameObject gameOverUI;
    public PlayerMovement playerMovement;
    public MonoBehaviour playerShoot;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private float lerpTimer;
    private bool isDead = false;

    void Start()
    {
        health = maxHealth;
        playerRespawn = GetComponent<PlayerRespawn>();

        if (livesUI != null)
        {
            livesUI.UpdateLives(currentLives);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        frontHealthBar.fillAmount = health / maxHealth;
        backHealthBar.fillAmount = health / maxHealth;

        healthText.text = health.ToString();

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    void Update()
    {
        HealthBarUpdate();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        health = Mathf.Clamp(health, 0f, maxHealth);
        healthText.text = health.ToString();
        lerpTimer = 0f;

        if (health <= 0f)
        {
            LoseLife();
        }
        else
        {
            PlaySound(hurtSound, 0.8f);
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        health += amount;
        health = Mathf.Clamp(health, 0f, maxHealth);
        healthText.text = health.ToString();
        lerpTimer = 0f;

        health += amount; //add health and clamp it so that it never goes over maxHealth
        health = Mathf.Clamp(health, 0f, maxHealth);

        Debug.Log("Player healed! Current Health: " + health);
    }

    void HealthBarUpdate()
    {
        float healthPercent = health / maxHealth;
        frontHealthBar.fillAmount = healthPercent;

        lerpTimer += Time.deltaTime;
        float percentComplete = lerpTimer / chipSpeed;
        percentComplete = Mathf.Clamp01(percentComplete);

        backHealthBar.fillAmount = Mathf.Lerp(
            backHealthBar.fillAmount,
            healthPercent,
            percentComplete
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10f);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died!");

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShoot != null) playerShoot.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Run the audio pause/resume sequence
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // 1. Freeze enemy audio logic globally
        EnemyBase.suppressEnemyAudio = true;

        // 2. Find all playing enemy audio sources and record their original volumes
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Dictionary<AudioSource, float> initialVolumes = new Dictionary<AudioSource, float>();

        foreach (EnemyBase enemy in allEnemies)
        {
            AudioSource[] sources = enemy.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource s in sources)
            {
                if (s.isPlaying)
                {
                    initialVolumes[s] = s.volume;
                }
            }
        }

        float fadeDuration = 0.4f; // Duration in seconds for fade out and fade in

        // 3. Smoothly Fade Out enemy audio
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            foreach (var kvp in initialVolumes)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.volume = Mathf.Lerp(kvp.Value, 0f, t);
                }
            }
            yield return null;
        }

        // Pause all sources after they reach 0 volume
        foreach (var kvp in initialVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.Pause();
            }
        }

        // 4. Play death scream (2D full volume)
        if (deathSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0.0f;
            audioSource.volume = 1.0f;
            audioSource.PlayOneShot(deathSound, 1.0f);
        }

        // 5. Hold silence for the remaining duration
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, 3.0f - fadeDuration));

        // 6. Resume audio at 0 volume and Fade In back to original levels
        foreach (var kvp in initialVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.UnPause();
                kvp.Key.volume = 0f;
            }
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            foreach (var kvp in initialVolumes)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.volume = Mathf.Lerp(0f, kvp.Value, t);
                }
            }
            yield return null;
        }

        // Ensure volumes are restored precisely to their initial values
        foreach (var kvp in initialVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.volume = kvp.Value;
            }
        }

        // 7. Unfreeze enemy audio logic
        EnemyBase.suppressEnemyAudio = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void LoseLife()
    {


        currentLives--;

        Debug.Log("Player died! Lives remaining: " + currentLives);

        if (livesUI != null)
        {
            livesUI.UpdateLives(currentLives);
        }

        //make player respawn if there is still lives remaining, if not player die and game restart
        if (currentLives > 0)
        {
            RespawnPlayer();
        }
        else
        {
            Die();
        }
    }
    private void RespawnPlayer()
    {
        // Move player to checkpoint
        if (playerRespawn != null)
        {
            playerRespawn.Respawn();
        }

        // Stop player movement
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Restore health
        health = maxHealth;
        healthText.text = health.ToString();

        // Reset health bars
        frontHealthBar.fillAmount = 1f;
        backHealthBar.fillAmount = 1f;

        lerpTimer = 0f;
        isDead = false;

        // Enable movement
        if (playerMovement != null)
            playerMovement.enabled = true;

        // Enable shooting
        if (playerShoot != null)
            playerShoot.enabled = true;

        Debug.Log("Respawned at checkpoint!");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}