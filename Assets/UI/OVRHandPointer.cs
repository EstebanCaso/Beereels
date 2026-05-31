using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// Apunta a Canvas world-space con la pose del OVRHand y dispara clicks
/// con el pinch del �ndice. Mismo patr�n que SteeringWheel.cs (PointerPose
/// + GetFingerPinchStrength) para no introducir un sistema de input nuevo.
///
/// Requiere:
///  - Un EventSystem en la escena (sin StandaloneInputModule).
///  - Cada Canvas world-space debe tener GraphicRaycaster.
/// </summary>
[DefaultExecutionOrder(-50)]
public class OVRHandPointer : MonoBehaviour
{
    [Header("Mano")]
    public OVRHand hand;

    [Header("Visuales")]
    public LineRenderer aimLine;
    public Transform reticle;
    public float maxRayDistance = 5f;

    [Header("Pinch")]
    [Range(0f, 1f)] public float pinchEnterThreshold = 0.7f;
    [Range(0f, 1f)] public float pinchExitThreshold = 0.4f;

    private PointerEventData _pointer;
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();
    private GameObject _currentHover;
    private GameObject _pressTarget;
    private bool _isPinching;

    void Awake()
    {
        if (aimLine != null) aimLine.positionCount = 2;
    }

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null || hand == null || !hand.IsTracked)
        {
            HideVisuals();
            ReleaseHover();
            return;
        }

        if (_pointer == null) _pointer = new PointerEventData(es);

        Transform pose = hand.PointerPose;
        if (pose == null)
        {
            HideVisuals();
            ReleaseHover();
            return;
        }

        Vector3 origin = pose.position;
        Vector3 dir = pose.forward;

        _pointer.Reset();
        _pointer.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        _pointer.delta = Vector2.zero;
        _pointer.pointerCurrentRaycast = default;

        _hits.Clear();
        RaycastWorld(origin, dir, _hits);

        RaycastResult bestHit = default;
        bool hasHit = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < _hits.Count; i++)
        {
            float d = _hits[i].distance;
            if (d < bestDistance)
            {
                bestDistance = d;
                bestHit = _hits[i];
                hasHit = true;
            }
        }

        Vector3 endPoint = origin + dir * maxRayDistance;
        GameObject hoverTarget = null;

        if (hasHit)
        {
            _pointer.pointerCurrentRaycast = bestHit;
            endPoint = bestHit.worldPosition != Vector3.zero
                ? bestHit.worldPosition
                : origin + dir * bestHit.distance;
            hoverTarget = bestHit.gameObject;
        }

        UpdateVisuals(origin, endPoint, hasHit);
        UpdateHover(hoverTarget);
        UpdatePinch(hoverTarget);
    }

    private void RaycastWorld(Vector3 origin, Vector3 dir, List<RaycastResult> results)
    {
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
        for (int i = 0; i < raycasters.Length; i++)
        {
            GraphicRaycaster gr = raycasters[i];
            if (gr == null || !gr.enabled || !gr.gameObject.activeInHierarchy) continue;

            Canvas c = gr.GetComponent<Canvas>();
            if (c == null || c.renderMode != RenderMode.WorldSpace) continue;

            // Para que GraphicRaycaster.Raycast funcione con un rayo arbitrario,
            // proyectamos manualmente cada Graphic del Canvas contra el rayo.
            RaycastGraphics(c, origin, dir, results);
        }
    }

    private void RaycastGraphics(Canvas canvas, Vector3 origin, Vector3 dir, List<RaycastResult> results)
    {
        IList<Graphic> graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
        if (graphics == null) return;

        Transform canvasTf = canvas.transform;
        Vector3 canvasNormal = canvasTf.forward;
        Vector3 canvasOrigin = canvasTf.position;

        Plane plane = new Plane(canvasNormal, canvasOrigin);
        if (!plane.Raycast(new Ray(origin, dir), out float enter)) return;
        if (enter < 0f || enter > maxRayDistance) return;

        Vector3 worldHit = origin + dir * enter;

        for (int i = 0; i < graphics.Count; i++)
        {
            Graphic g = graphics[i];
            if (g == null || !g.raycastTarget || !g.gameObject.activeInHierarchy) continue;
            if (g.depth == -1) continue;

            RectTransform rt = g.rectTransform;
            Vector3 localHit = rt.InverseTransformPoint(worldHit);
            Rect r = rt.rect;
            if (!r.Contains(new Vector2(localHit.x, localHit.y))) continue;

            results.Add(new RaycastResult
            {
                gameObject = g.gameObject,
                module = canvas.GetComponent<GraphicRaycaster>(),
                distance = enter,
                worldPosition = worldHit,
                worldNormal = canvasNormal,
                screenPosition = WorldToFakeScreen(worldHit),
                index = results.Count,
                depth = g.depth,
                sortingLayer = canvas.sortingLayerID,
                sortingOrder = canvas.sortingOrder
            });
        }
    }

    private static Vector2 WorldToFakeScreen(Vector3 worldPoint)
    {
        // El EventSystem usa coordenadas 2D en algunos paths; damos algo
        // estable basado en la posici�n de impacto para evitar NaN.
        return new Vector2(worldPoint.x * 100f, worldPoint.y * 100f);
    }

    private void UpdateHover(GameObject newHover)
    {
        if (newHover == _currentHover) return;

        if (_currentHover != null)
            ExecuteEvents.Execute(_currentHover, _pointer, ExecuteEvents.pointerExitHandler);

        _currentHover = newHover;
        _pointer.pointerEnter = newHover;

        if (_currentHover != null)
            ExecuteEvents.Execute(_currentHover, _pointer, ExecuteEvents.pointerEnterHandler);
    }

    private void ReleaseHover()
    {
        if (_currentHover != null && _pointer != null)
            ExecuteEvents.Execute(_currentHover, _pointer, ExecuteEvents.pointerExitHandler);
        _currentHover = null;
        _pressTarget = null;
        _isPinching = false;
    }

    private void UpdatePinch(GameObject hoverTarget)
    {
        float pinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        bool wasPinching = _isPinching;

        if (!wasPinching && pinch > pinchEnterThreshold) _isPinching = true;
        else if (wasPinching && pinch < pinchExitThreshold) _isPinching = false;

        if (_isPinching && !wasPinching)
        {
            _pressTarget = hoverTarget;
            if (_pressTarget != null)
            {
                _pointer.pressPosition = _pointer.position;
                _pointer.pointerPressRaycast = _pointer.pointerCurrentRaycast;
                GameObject pressed = ExecuteEvents.ExecuteHierarchy(_pressTarget, _pointer, ExecuteEvents.pointerDownHandler);
                if (pressed == null) pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pressTarget);
                _pointer.pointerPress = pressed;
                _pointer.rawPointerPress = _pressTarget;
            }
        }
        else if (!_isPinching && wasPinching)
        {
            if (_pressTarget != null)
            {
                ExecuteEvents.Execute(_pressTarget, _pointer, ExecuteEvents.pointerUpHandler);

                GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pressTarget);
                if (clickHandler != null && clickHandler == _pointer.pointerPress)
                    ExecuteEvents.Execute(clickHandler, _pointer, ExecuteEvents.pointerClickHandler);
            }
            _pointer.pointerPress = null;
            _pointer.rawPointerPress = null;
            _pressTarget = null;
        }
    }

    private void UpdateVisuals(Vector3 origin, Vector3 end, bool hasHit)
    {
        if (aimLine != null)
        {
            aimLine.enabled = true;
            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, end);
        }
        if (reticle != null)
        {
            reticle.gameObject.SetActive(hasHit);
            if (hasHit) reticle.position = end;
        }
    }

    private void HideVisuals()
    {
        if (aimLine != null) aimLine.enabled = false;
        if (reticle != null) reticle.gameObject.SetActive(false);
    }
    public void SetActive(bool active)
    {
        enabled = active;
        if (!active) HideVisuals();
    }
}
