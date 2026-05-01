using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Botones del men� principal. Conectar OnStart y OnQuit a los Button.onClick.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Escenas")]
    public string gameSceneName = "SampleScene";

    public void OnStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
