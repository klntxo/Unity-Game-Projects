using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [Header("Player Setup")]
    [Tooltip("Drag Mario's PlayerController script component here")]
    public MonoBehaviour playerControllerScript;

    [Header("Spawner Setup")]
    [Tooltip("Drag the GameObject that handles spawning barrels here")]
    public GameObject barrelSpawner;

    [Header("Scene Management")]
    [Tooltip("Make sure this exact scene name is added to your Build Settings!")]
    public string winScreenSceneName = "WinScreen";

    void Start()
    {
        // i. Start the level loop immediately upon loading
        StartCoroutine(StartLevelCountdown());
    }

    IEnumerator StartLevelCountdown()
    {
        // Turn off Mario's movement script at birth
        if (playerControllerScript != null) playerControllerScript.enabled = false;

        // Turn off the barrel spawner object at birth
        if (barrelSpawner != null) barrelSpawner.SetActive(false);

        // Wait exactly 3 seconds
        yield return new WaitForSeconds(3f);

        // Enable everything so the game actually starts
        if (playerControllerScript != null) playerControllerScript.enabled = true;
        if (barrelSpawner != null) barrelSpawner.SetActive(true);

        Debug.Log("3 seconds passed: Spawning started and Mario can move!");
    }

    // ii. Loss Condition: Call this when Mario hits a barrel or hazard
    public void PlayerDied()
    {
        Debug.Log("Mario hit an obstacle! Restarting level...");
        // Reloads whatever level is currently active
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // iii. Win Condition: Call this when Mario reaches the Princess
    public void PlayerWon()
    {
        Debug.Log("Mario reached the Princess! Loading win screen...");
        SceneManager.LoadScene(winScreenSceneName);
    }
}