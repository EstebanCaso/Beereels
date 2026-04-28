using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Atajos de teclado para probar la UI sin VR (Editor / build de PC).
/// Activar este script s�lo si Active Input Handling = Input System (New) o Both.
///   P  -> Toggle pause
///   G  -> Forzar Game Over (probar el panel)
///   R  -> Reiniciar la corrida
///   M  -> Volver al Main Menu
/// </summary>
public class DesktopDebugInput : MonoBehaviour
{
    public Key togglePauseKey = Key.P;
    public Key forceGameOverKey = Key.G;
    public Key restartKey = Key.R;
    public Key mainMenuKey = Key.M;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[togglePauseKey].wasPressedThisFrame)
        {
            if (PauseController.Instance != null) PauseController.Instance.Toggle();
        }
        if (kb[forceGameOverKey].wasPressedThisFrame)
        {
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        }
        if (kb[restartKey].wasPressedThisFrame)
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartRun();
        }
        if (kb[mainMenuKey].wasPressedThisFrame)
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }
    }
}
