using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Velocidad")]
    public float baseSpeed = 15f;
    public float maxSpeed = 35f;
    public float acceleration = 0.5f;

    [Header("Freno de mano")]
    public float handbrakeStrength = 8f;
    public bool handbrakeActive = false;

    [Header("Manejo Arcade")]
    public float steerSensitivity = 0.05f; // Ajusta este valor si el carro se mueve muy rápido a los lados

    // Estado interno
    private float _currentSpeed;
    private float _currentSteerAngle = 0f;
    private RoadGenerator _roadGenerator;
    private GameManager _gameManager;

    public float CurrentSpeed => _currentSpeed;

    void Start()
    {
        _currentSpeed = baseSpeed;
        _roadGenerator = FindObjectOfType<RoadGenerator>();
        _gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (_gameManager != null && _gameManager.IsGameOver) return;

        // 1. Control de Velocidad
        if (!handbrakeActive)
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        else
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, baseSpeed * 0.3f, handbrakeStrength * Time.deltaTime);

        // 2. Movimiento Arcade (Movimiento del mundo)
        if (_roadGenerator != null)
        {
            // Movemos la calle hacia atrás (ilusión de avanzar)
            _roadGenerator.transform.position += Vector3.back * _currentSpeed * Time.deltaTime;

            // Movemos la calle lateralmente basado en el volante
            // Multiplicamos por _currentSpeed para que no puedas "derrapar" lateralmente si estás detenido
            float lateralMove = _currentSteerAngle * steerSensitivity * _currentSpeed * Time.deltaTime;
            _roadGenerator.transform.position += Vector3.right * lateralMove;
        }
    }

    // El volante llamará a esta función en cada frame
    public void SetSteerAngle(float angle)
    {
        // Interpola suavemente para evitar movimientos bruscos de la carretera
        _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, angle, Time.deltaTime * 10f);
    }

    public void ActivateHandbrake(bool active)
    {
        handbrakeActive = active;
    }
}