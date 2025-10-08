using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Pause Menu UI")]
    [Tooltip("Assign your Pause Menu Canvas here.")]
    public GameObject pauseMenuUI;

    private bool isPaused = false;

    void Awake()
    {
        // Singleton pattern (persists between scenes)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Toggle pause with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // ============================================================
    // 🔹 SCENE CONTROL
    // ============================================================

    /// <summary>
    /// Loads a scene by name (must match Build Settings).
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Time.timeScale = 1f; // unpause before loading
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager.LoadScene() called with no scene name!");
        }
    }

    /// <summary>
    /// Reloads the current active scene.
    /// </summary>
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Quits the game (works in builds and exits Play mode in editor).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("🛑 Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    // 🔹 PAUSE / RESUME CONTROL
    // ============================================================

    public void PauseGame()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f; // freeze gameplay
        isPaused = true;
        Debug.Log("⏸️ Game Paused");
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f; // resume gameplay
        isPaused = false;
        Debug.Log("▶️ Game Resumed");
    }
}
