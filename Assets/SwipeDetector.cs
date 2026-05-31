using UnityEngine;
using System;

public class SwipeDetector : MonoBehaviour
{
    [Header("Configuración")]
    public OVRHand rightHand;
    public float swipeVelocityThreshold = 0.4f;

    public event Action<float> OnSwipe; // float: positivo = arriba, negativo = abajo

    private Vector3 _lastThumbPos;
    private bool _thumbInside = false;
    private bool _initialized = false;

    void OnEnable()
    {
        _initialized = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // El pulgar entró en la pantalla
        if (other.gameObject.name.Contains("thumb_finger_tip") ||
            other.gameObject.name.Contains("thumb3"))
        {
            _thumbInside = true;
            _initialized = false; // resetea para no contar el primer frame
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.Contains("thumb_finger_tip") ||
            other.gameObject.name.Contains("thumb3"))
        {
            _thumbInside = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.name.Contains("thumb_finger_tip") &&
            !other.gameObject.name.Contains("thumb3")) return;

        Vector3 currentPos = other.transform.position;

        if (!_initialized)
        {
            _lastThumbPos = currentPos;
            _initialized = true;
            return;
        }

        float velocityY = (currentPos.y - _lastThumbPos.y) / Time.deltaTime;
        _lastThumbPos = currentPos;

        if (Mathf.Abs(velocityY) > swipeVelocityThreshold)
        {
            OnSwipe?.Invoke(velocityY);
        }
    }
}