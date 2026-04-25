using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;

    void Start()
    {
        gameOverPanel.SetActive(false);
        GameManager.Instance.onGameOver.AddListener(ShowGameOver);
    }

    void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        scoreText.text = $"Score: {GameManager.Instance.GetScore():F0}";
        timeText.text = $"Tiempo: {GameManager.Instance.GetSurvivalTime():F1}s";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}