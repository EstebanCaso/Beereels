using UnityEngine;

public class SteeringWheel : MonoBehaviour
{
    [Header("Manos (OVR)")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    [Header("Visuales de Manos (Geometría fantasmas)")]
    public Transform leftHandVisual;
    public Transform rightHandVisual;
    public float followTightness = 25f;

    [Header("Configuración del Volante")]
    public float wheelMaxRotation = 270f;
    public float followSmoothing = 25f;
    public float returnSpeed = 150f;
    public float returnDamping = 0.85f;

    [Header("Configuración de Agarre")]
    public float grabRadius = 0.35f;
    public float depthTolerance = 0.25f;
    public float pinchThreshold = 0.2f;

    [Header("Conexión al Carro")]
    public CarController carController;
    public float maxSteerAngleOut = 35f; // Rango máximo que se enviará al CarController (-35 a 35)

    // Estado interno
    private bool _leftGrabbing;
    private bool _rightGrabbing;
    private int _previousGrabCount = 0;

    private Vector3 _leftPos;
    private Vector3 _rightPos;
    private Vector3 _leftGrabOffset;
    private Vector3 _rightGrabOffset;
    private bool _leftWasGrabbing;
    private bool _rightWasGrabbing;

    private float _currentWheelAngle;
    private float _displayWheelAngle;
    private float _grabReferenceAngle;
    private float _wheelAngleOnGrab;
    private float _returnVelocity;

    void Update()
    {
        UpdateGrabState();
        UpdateHandAttachment();
        CalculateWheelRotation();
        ApplyVisuals();
        ApplyToCar();
    }

    private void UpdateGrabState()
    {
        _leftGrabbing = DetectGrab(leftHand, ref _leftPos);
        _rightGrabbing = DetectGrab(rightHand, ref _rightPos);
    }

    private bool DetectGrab(OVRHand hand, ref Vector3 pos)
    {
        if (hand == null || !hand.IsTracked) return false;

        pos = hand.PointerPose.position;
        // Transformamos la posición de la mano al ESPACIO LOCAL del volante
        Vector3 localPos = transform.InverseTransformPoint(pos);
        float dist = new Vector2(localPos.x, localPos.y).magnitude;

        bool nearWheel = dist < grabRadius && Mathf.Abs(localPos.z) < depthTolerance;
        if (!nearWheel) return false;

        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;
    }

    private void CalculateWheelRotation()
    {
        int currentGrabCount = (_leftGrabbing ? 1 : 0) + (_rightGrabbing ? 1 : 0);

        if (currentGrabCount > 0)
        {
            float handAngle = GetLocalHandAngle();

            // EVITAR SALTOS: Si el jugador suelta una mano o agrega la segunda, recalculamos la base
            if (currentGrabCount != _previousGrabCount)
            {
                _grabReferenceAngle = handAngle;
                _wheelAngleOnGrab = _currentWheelAngle;
                _returnVelocity = 0f;
            }

            float delta = Mathf.DeltaAngle(_grabReferenceAngle, handAngle);
            float targetAngle = Mathf.Clamp(_wheelAngleOnGrab + delta, -wheelMaxRotation, wheelMaxRotation);

            _currentWheelAngle = Mathf.Lerp(_currentWheelAngle, targetAngle, Time.deltaTime * followSmoothing);
        }
        else
        {
            // Auto-retorno (Efecto resorte hacia el centro)
            float force = -_currentWheelAngle * returnSpeed * Time.deltaTime;
            _returnVelocity += force;
            _returnVelocity *= returnDamping;
            _currentWheelAngle += _returnVelocity * Time.deltaTime;

            if (Mathf.Abs(_currentWheelAngle) < 0.5f && Mathf.Abs(_returnVelocity) < 1f)
            {
                _currentWheelAngle = 0f;
                _returnVelocity = 0f;
            }
        }

        _previousGrabCount = currentGrabCount;
    }

    private float GetLocalHandAngle()
    {
        Vector3 localLeft = transform.InverseTransformPoint(_leftPos);
        Vector3 localRight = transform.InverseTransformPoint(_rightPos);

        // Si agarra con dos manos, el vector es de la mano izquierda a la derecha
        if (_leftGrabbing && _rightGrabbing)
        {
            Vector3 dir = localRight - localLeft;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        // Si agarra con una, el vector es del centro del volante a la mano
        Vector3 activeHandLocal = _rightGrabbing ? localRight : localLeft;
        return Mathf.Atan2(activeHandLocal.y, activeHandLocal.x) * Mathf.Rad2Deg;
    }

    private void UpdateHandAttachment()
    {
        HandleHandAttach(_leftGrabbing, ref _leftWasGrabbing, ref _leftGrabOffset, _leftPos, leftHandVisual);
        HandleHandAttach(_rightGrabbing, ref _rightWasGrabbing, ref _rightGrabOffset, _rightPos, rightHandVisual);
    }

    private void HandleHandAttach(bool isGrabbing, ref bool wasGrabbing, ref Vector3 grabOffset, Vector3 handWorldPos, Transform handVisual)
    {
        if (handVisual == null) return;

        if (isGrabbing)
        {
            if (!wasGrabbing)
            {
                grabOffset = transform.InverseTransformPoint(handWorldPos);
                wasGrabbing = true;
            }

            Vector3 targetPos = transform.TransformPoint(grabOffset);
            handVisual.position = Vector3.Lerp(handVisual.position, targetPos, Time.deltaTime * followTightness);

            // Mantener la mano visual orientada hacia el giro del volante
            Vector3 forwardDir = transform.forward;
            Vector3 upDir = (targetPos - transform.position).normalized;
            handVisual.rotation = Quaternion.Lerp(handVisual.rotation, Quaternion.LookRotation(forwardDir, upDir), Time.deltaTime * followTightness);
        }
        else
        {
            wasGrabbing = false;
        }
    }

    private void ApplyVisuals()
    {
        // Gira la malla 3D del volante en Unity
        _displayWheelAngle = Mathf.Lerp(_displayWheelAngle, _currentWheelAngle, Time.deltaTime * followSmoothing);
        transform.localRotation = Quaternion.Euler(0f, 0f, _displayWheelAngle);
    }

    private void ApplyToCar()
    {
        if (carController == null) return;

        float normalized = _currentWheelAngle / wheelMaxRotation; // De -1 a 1
        float targetSteer = normalized * maxSteerAngleOut;

        carController.SetSteerAngle(targetSteer);
    }
}