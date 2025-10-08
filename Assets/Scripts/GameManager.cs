using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Pause Menu UI (optional)")]
    public GameObject pauseMenuUI;

    [HideInInspector] public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (pauseMenuUI != null && Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    // ============================================================
    // Scene Management
    // ============================================================
    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ LoadScene called with no scene name!");
        }
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    // Pause System
    // ============================================================
    public void PauseGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            IsPaused = true;
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }

    // ============================================================
    // Scene Loaded Handler
    // ============================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset pause menu
        FindPauseMenuInScene();

        // Hook Main Menu buttons automatically if in that scene
        if (scene.name == "MainMenu")
        {
            Button[] buttons = GameObject.FindObjectsOfType<Button>();
            foreach (Button b in buttons)
            {
                string btnName = b.gameObject.name.ToLower();
                b.onClick.RemoveAllListeners(); // clear old listeners

                if (btnName.Contains("start"))
                    b.onClick.AddListener(() => LoadScene("LevelSelect"));
                else if (btnName.Contains("credits"))
                    b.onClick.AddListener(() => LoadScene("Credits"));
                else if (btnName.Contains("quit"))
                    b.onClick.AddListener(QuitGame);
            }
        }

        // Make sure the game is not paused when loading any scene
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void FindPauseMenuInScene()
    {
        GameObject found = GameObject.FindWithTag("PauseMenu");
        if (found != null)
        {
            pauseMenuUI = found;
            pauseMenuUI.SetActive(false);
        }
        else
        {
            pauseMenuUI = null; // no pause menu in this scene
        }
    }
}
