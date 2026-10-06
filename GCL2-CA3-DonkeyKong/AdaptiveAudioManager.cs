using UnityEngine;

public class AdaptiveAudioManager : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("Drag Mario here")]
    public Transform player;

    [Header("Audio Sources")]
    [Tooltip("AudioSource playing the calm/bottom floor music")]
    public AudioSource baseTrack;

    [Tooltip("AudioSource playing the intense/top floor music")]
    public AudioSource intenseTrack;

    [Header("Height Settings")]
    [Tooltip("The Y position of the very bottom floor")]
    public float minY = -4f;

    [Tooltip("The Y position of the top floor near the Princess")]
    public float maxY = 10f;

    void Start()
    {
        // Force both tracks to loop and play at the exact same time so they stay perfectly in sync
        if (baseTrack != null && intenseTrack != null)
        {
            baseTrack.loop = true;
            intenseTrack.loop = true;

            baseTrack.Play();
            intenseTrack.Play();

            // Start with the intense track muted
            intenseTrack.volume = 0f;
        }
    }

    void Update()
    {
        if (player == null || baseTrack == null || intenseTrack == null) return;

        // Calculate how high Mario is as a percentage between the bottom and top floor (0.0 to 1.0)
        float currentY = player.position.y;
        float percentage = Mathf.Clamp01((currentY - minY) / (maxY - minY));

        // Smoothly crossfade the volumes based on his height
        baseTrack.volume = 1f - percentage;
        intenseTrack.volume = percentage;
    }
}