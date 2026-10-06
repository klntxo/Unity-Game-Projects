using UnityEngine;
using UnityEngine.SceneManagement;

public class Princess : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Must match the scene name in File -> Build Settings exactly")]
    public string winSceneName = "WinScreen";

    [Header("Distance Backup")]
    public float winDistanceThreshold = 1.2f;

    private bool hasWon = false;

    // Method 1: Standard 2D Trigger Collision
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;

        if (other.CompareTag("Player"))
        {
            TriggerWin();
        }
    }

    // Method 2: Position Distance Fallback (Fires if physics colliders fail)
    private void Update()
    {
        if (hasWon) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= winDistanceThreshold)
            {
                TriggerWin();
            }
        }
    }

    private void TriggerWin()
    {
        hasWon = true;
        Debug.Log("Win condition reached! Loading " + winSceneName);

        // Try using LevelManager if your project has one
        LevelManager manager = FindObjectOfType<LevelManager>();
        if (manager != null)
        {
            manager.PlayerWon();
        }
        else
        {
            // Direct scene load fallback
            SceneManager.LoadScene(winSceneName);
        }
    }
}