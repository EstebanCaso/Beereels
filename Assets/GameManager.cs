using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controla el estado global del juego: puntos, game over, tiempo.
/// Los otros sistemas le reportan a este.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Estado")]
    public bool IsGameOver { get; private set; } = false;

    [Header("Puntos")]
    public float pointsPerSecondAlive = 1f;        // Puntos por sobrevivir
    public float pointsPerReelSecond = 5f;          // Puntos por ver reels
    public float pointsPerBeerSip = 50f;            // Puntos por tomar cerveza
    public float penaltyPerRoadSecond = 3f;         // Penalización por ver la carretera

    // Estado de puntos
    private float _totalScore = 0f;
    private float _survivalTime = 0f;

    // Estado de "qué está haciendo el jugador ahora"
    private bool _isLookingAtPhone = false;
    private bool _isLookingAtRoad = false;

    // Evento que otros scripts escuchan cuando hay game over
    public UnityEvent onGameOver = new UnityEvent();

    // Singleton simple para que otros scripts lo encuentren fácil
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (IsGameOver) return;

        _survivalTime += Time.deltaTime;

        // Puntos base por sobrevivir
        _totalScore += pointsPerSecondAlive * Time.deltaTime;

        // Bonus por ver el celular
        if (_isLookingAtPhone)
            _totalScore += pointsPerReelSecond * Time.deltaTime;

        // Penalización por ver la carretera
        if (_isLookingAtRoad)
            _totalScore -= penaltyPerRoadSecond * Time.deltaTime;

        _totalScore = Mathf.Max(0f, _totalScore); // No puede ir negativo
    }

    // Llamado desde el sistema de detección de mirada
    public void SetLookingAtPhone(bool looking) => _isLookingAtPhone = looking;
    public void SetLookingAtRoad(bool looking) => _isLookingAtRoad = looking;

    // Llamado cuando el jugador toma cerveza
    public void AddBeerPoints() => _totalScore += pointsPerBeerSip;

    // Llamado por el sistema de colisiones
    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log($"GAME OVER — Score: {_totalScore:F0} — Tiempo: {_survivalTime:F1}s");

        // Detenemos el tiempo del juego
        Time.timeScale = 0f;

        onGameOver.Invoke();
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public float GetScore() => _totalScore;
    public float GetSurvivalTime() => _survivalTime;
}