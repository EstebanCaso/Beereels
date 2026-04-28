using UnityEngine;
using TMPro;

/// <summary>
/// HUD m�nimo: muestra el puntaje (y opcionalmente la velocidad)
/// le�dos del GameManager / CarController.
/// El Canvas se parentea al centerEyeAnchor del OVRCameraRig
/// para que siga la mirada del jugador sin c�digo extra.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Texto")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI speedText; // opcional

    [Header("Fuente de velocidad (opcional)")]
    public CarController car;

    void Update()
    {
        if (scoreText != null && GameManager.Instance != null)
            scoreText.text = $"{GameManager.Instance.GetScore():F0}";

        if (speedText != null && car != null)
            speedText.text = $"{car.CurrentSpeed:F0} km/h";
    }
}
