using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el panel de Game Over y reporta puntaje y tiempo.
/// Los botones del panel (Restart, Main Menu) se conectan en el Inspector
/// directamente a GameManager.RestartRun() y GameManager.LoadMainMenu().
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.onGameOver.AddListener(ShowGameOver);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (scoreText != null) scoreText.text = $"Score: {GameManager.Instance.GetScore():F0}";
        if (timeText != null) timeText.text = $"Tiempo: {GameManager.Instance.GetSurvivalTime():F1}s";
    }
}
