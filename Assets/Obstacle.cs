using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verificamos que sea el jugador el que chocó
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.TriggerGameOver();
        }
    }
}