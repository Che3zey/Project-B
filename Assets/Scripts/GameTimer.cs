using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // <-- TextMeshPro namespace

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float totalTime = 60f;
    private float remainingTime;

    [Header("UI")]
    public TMP_Text timerText; // <-- Use TMP_Text instead of Text

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != "Level1" && sceneName != "Level2" && sceneName != "Level3")
        {
            gameObject.SetActive(false);
            return;
        }

        remainingTime = totalTime;
        UpdateTimerUI();
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            SceneManager.LoadScene("GameOver");
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
