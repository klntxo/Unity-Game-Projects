using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayAmbience;

    [Header("Settings")]
    public float fadeDuration = 1.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D Sound (unaffected by 3D position)
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            CrossfadeTo(mainMenuMusic);
        }
        else if (scene.name == "Level01")
        {
            CrossfadeTo(gameplayAmbience);
        }
    }

    public void CrossfadeTo(AudioClip newClip)
    {
        if (audioSource.clip == newClip && audioSource.isPlaying) return;
        StartCoroutine(FadeTrackRoutine(newClip));
    }

    private IEnumerator FadeTrackRoutine(AudioClip newClip)
    {
        // Smooth Fade Out current track
        if (audioSource.isPlaying)
        {
            float startVol = audioSource.volume;
            for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
        }

        audioSource.clip = newClip;

        // Smooth Fade In new track
        if (newClip != null)
        {
            audioSource.Play();
            for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            audioSource.volume = 1f;
        }
        else
        {
            audioSource.Stop();
        }
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}