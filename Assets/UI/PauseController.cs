using UnityEngine;

/// <summary>
/// Controla la pausa del juego. Una sola instancia por escena.
///
/// - Toggle() congela/reanuda Time.timeScale y muestra/oculta el panel.
/// - El panel debe vivir dentro del rig para que aparezca delante del jugador.
/// - Los botones del panel se conectan a Resume() / RestartRun() / BackToMainMenu().
/// - El bot�n de mu�eca llama a Toggle() (v�a OVRHandPointer + Button.onClick).
/// - Bloqueado durante Game Over para evitar estados inv�lidos.
/// </summary>
public class PauseController : MonoBehaviour
{
    public static PauseController Instance { get; private set; }

    [Header("Referencias")]
    public GameObject pausePanel;

    [Header("Opcional: ocultar HUD al pausar")]
    public GameObject hudToHide;

    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void Toggle()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (hudToHide != null) hudToHide.SetActive(false);
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (hudToHide != null) hudToHide.SetActive(true);
    }

    // Atajos para conectar directamente desde botones del panel:
    public void RestartRun()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.RestartRun();
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}
