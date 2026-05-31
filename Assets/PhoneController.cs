using Oculus.Interaction.PoseDetection;
using UnityEngine;
using UnityEngine.Video;

public class PhoneController : MonoBehaviour
{
    [Header("Manos (OVR)")]
    public OVRHand rightHand;

    [Header("Posición en mano")]
    public Vector3 handOffset = new Vector3(0f, 0.05f, 0.1f); //cambiar valores desde el inspector, ahoreita cuando lo aharro la parte de abajo se acomoda a la izquierda y el de arriba a la derexha de manera horizontal.
   
    [Header("Configuración de agarre")]
    public float grabRadius = 0.2f;
    [Range(-1f, 0f)]
    [Tooltip("Qué tan plana debe estar la palma. -0.3 = tolerante, -0.7 = estricto")]
    public float palmFlatThreshold = -0.3f;

    [Header("Swipe Detector")]
    public SwipeDetector swipeDetector;

    [Header("Debug Visual")]
    public Renderer phoneRenderer;

    [Header("Videos")]
    public VideoClip[] reels;

    [Header("Referencias")]
    public VideoPlayer videoPlayer;
    public GameManager gameManager;

    // Estado
    private bool _isHeld = false;
    private int _currentReelIndex = 0;
    private Vector3 _phoneRestPosition;
    private Quaternion _phoneRestRotation;
    private bool _canSwipe = true;
    private float _swipeCooldown = 0.7f;
    private float _swipeTimer = 0f;
    private Vector3 _grabOffset;
    private Quaternion _grabRotOffset;

    void Start()
    {
        _phoneRestPosition = transform.localPosition;
        _phoneRestRotation = transform.localRotation;

        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioMute(0, true);
            videoPlayer.Prepare();
        }

        if (swipeDetector != null)
        {
            swipeDetector.enabled = false;
            swipeDetector.OnSwipe += HandleSwipe;
        }
    }

    void OnDestroy()
    {
        if (swipeDetector != null)
            swipeDetector.OnSwipe -= HandleSwipe;
    }

    void Update()
    {
        UpdateGrab();

        if (_isHeld)
            gameManager?.SetLookingAtPhone(true);

        if (!_canSwipe)
        {
            _swipeTimer += Time.deltaTime;
            if (_swipeTimer >= _swipeCooldown)
            {
                _canSwipe = true;
                _swipeTimer = 0f;
            }
        }
    }

    void UpdateGrab()
    {
        if (rightHand == null || !rightHand.IsTracked)
        {
            SetDebugColor(Color.white);
            return;
        }

        Vector3 handPos = rightHand.PointerPose.position;
        float dist = Vector3.Distance(handPos, transform.position);
        bool nearPhone = dist < grabRadius;
        bool isPalmFlat = DetectPalmFlat();

        // Debug visual
        if (_isHeld)
            SetDebugColor(Color.blue);
        else if (nearPhone && isPalmFlat)
            SetDebugColor(Color.green);
        else if (nearPhone)
            SetDebugColor(Color.yellow);
        else
            SetDebugColor(Color.red);

        if (!_isHeld && nearPhone && isPalmFlat)
        {
            // Offset en espacio local de la mano — siempre frente a la palma
            _grabOffset = rightHand.PointerPose.rotation * handOffset;

            // Rotación fija relativa a la mano — la pantalla mira hacia tu cara
            _grabRotOffset = Quaternion.Inverse(rightHand.PointerPose.rotation)
                * Quaternion.Euler(90f, 0f, 0f);

            PickUpPhone();
        }
        else if (_isHeld && !isPalmFlat)
        {
            PutDownPhone();
        }

        if (_isHeld)
            UpdatePhoneInHand();
    }

    bool DetectPalmFlat()
    {
        if (rightHand == null || !rightHand.IsTracked) return false;

        // PointerPose.up apunta hacia donde mira la palma
        Vector3 palmUp = rightHand.PointerPose.rotation * Vector3.up;

        // dot < threshold = palma mirando hacia abajo = palma plana sobre el celular
        float dot = Vector3.Dot(palmUp, Vector3.up);
        return dot < palmFlatThreshold;
    }

    void SetDebugColor(Color color)
    {
        if (phoneRenderer != null)
            phoneRenderer.material.color = color;
    }

    void PickUpPhone()
    {
        _isHeld = true;
        StopAllCoroutines();

        if (videoPlayer != null)
        {
            videoPlayer.clip = reels[_currentReelIndex];
            videoPlayer.SetDirectAudioMute(0, false);
            videoPlayer.targetTexture.Release();
            videoPlayer.Play();
        }

        if (swipeDetector != null)
            swipeDetector.enabled = true;
    }

    void PutDownPhone()
    {
        _isHeld = false;

        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioMute(0, true);
            videoPlayer.Pause();
        }

        gameManager?.SetLookingAtPhone(false);

        if (swipeDetector != null)
            swipeDetector.enabled = false;

        ReturnPhoneToSeat();
    }

    void UpdatePhoneInHand()
    {
        Vector3 targetPos = rightHand.PointerPose.position + _grabOffset;
        Quaternion targetRot = rightHand.PointerPose.rotation * _grabRotOffset;

        transform.position = Vector3.Lerp(
            transform.position, targetPos, Time.deltaTime * 20f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation, targetRot, Time.deltaTime * 20f);
    }

    void ReturnPhoneToSeat()
    {
        StartCoroutine(ReturnCoroutine());
    }

    System.Collections.IEnumerator ReturnCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.5f;
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.Lerp(startPos, _phoneRestPosition, smooth);
            transform.localRotation = Quaternion.Lerp(startRot, _phoneRestRotation, smooth);
            yield return null;
        }

        transform.localPosition = _phoneRestPosition;
        transform.localRotation = _phoneRestRotation;
    }

    void HandleSwipe(float direction)
    {
        if (!_canSwipe || !_isHeld) return;
        if (direction > 0) NextReel();
        else PreviousReel();
        _canSwipe = false;
        _swipeTimer = 0f;
    }

    void NextReel()
    {
        _currentReelIndex = (_currentReelIndex + 1) % reels.Length;
        PlayReel(_currentReelIndex);
    }

    void PreviousReel()
    {
        _currentReelIndex = (_currentReelIndex - 1 + reels.Length) % reels.Length;
        PlayReel(_currentReelIndex);
    }

    void PlayReel(int index)
    {
        if (videoPlayer == null || reels.Length == 0) return;
        videoPlayer.clip = reels[index];
        videoPlayer.Play();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}