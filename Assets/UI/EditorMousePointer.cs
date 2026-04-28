using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Versi�n "flat" del OVRHandPointer para previsualizar la UI sin VR.
/// Usa la c�mara y el rat�n para apuntar a Canvas world-space y dispara
/// click con el bot�n izquierdo del rat�n.
///
/// Si hay alg�n OVRHandPointer con la mano siendo trackeada, se desactiva
/// solo para no pelear con el input real.
///
/// Requiere el paquete com.unity.inputsystem (ya est� en el proyecto).
/// </summary>
[DefaultExecutionOrder(-50)]
public class EditorMousePointer : MonoBehaviour
{
    [Header("C�mara")]
    [Tooltip("Si est� vac�o, usa Camera.main, y si tampoco hay, la primera c�mara activa.")]
    public Camera referenceCamera;

    [Header("Visuales")]
    public LineRenderer aimLine;
    public Transform reticle;
    public float maxRayDistance = 5f;

    private PointerEventData _pointer;
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();
    private GameObject _currentHover;
    private GameObject _pressTarget;
    private bool _wasPressed;

    void Awake()
    {
        if (aimLine != null) aimLine.positionCount = 2;
    }

    void Update()
    {
        if (HasTrackedHand())
        {
            HideVisuals();
            ReleaseHover();
            return;
        }

        EventSystem es = EventSystem.current;
        Camera cam = ResolveCamera();
        Mouse mouse = Mouse.current;
        if (es == null || cam == null || mouse == null)
        {
            HideVisuals();
            ReleaseHover();
            return;
        }

        if (_pointer == null) _pointer = new PointerEventData(es);

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        _pointer.Reset();
        _pointer.position = mousePos;
        _pointer.delta = Vector2.zero;
        _pointer.pointerCurrentRaycast = default;

        _hits.Clear();
        RaycastWorld(ray.origin, ray.direction, _hits);

        RaycastResult best = default;
        bool hasHit = false;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < _hits.Count; i++)
        {
            if (_hits[i].distance < bestDist)
            {
                bestDist = _hits[i].distance;
                best = _hits[i];
                hasHit = true;
            }
        }

        Vector3 endPoint = ray.origin + ray.direction * maxRayDistance;
        GameObject hoverTarget = null;
        if (hasHit)
        {
            _pointer.pointerCurrentRaycast = best;
            endPoint = best.worldPosition != Vector3.zero ? best.worldPosition : ray.origin + ray.direction * best.distance;
            hoverTarget = best.gameObject;
        }

        UpdateVisuals(ray.origin, endPoint, hasHit);
        UpdateHover(hoverTarget);
        UpdateClick(hoverTarget, mouse.leftButton.isPressed);
    }

    private Camera ResolveCamera()
    {
        if (referenceCamera != null) return referenceCamera;
        if (Camera.main != null) return Camera.main;
        Camera[] all = Camera.allCameras;
        return all.Length > 0 ? all[0] : null;
    }

    private static bool HasTrackedHand()
    {
        OVRHandPointer[] pointers = FindObjectsOfType<OVRHandPointer>();
        for (int i = 0; i < pointers.Length; i++)
        {
            if (pointers[i] == null) continue;
            if (pointers[i].hand != null && pointers[i].hand.IsTracked) return true;
        }
        return false;
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
                screenPosition = _pointer.position,
                index = results.Count,
                depth = g.depth,
                sortingLayer = canvas.sortingLayerID,
                sortingOrder = canvas.sortingOrder
            });
        }
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
        _wasPressed = false;
    }

    private void UpdateClick(GameObject hoverTarget, bool pressed)
    {
        if (pressed && !_wasPressed)
        {
            _pressTarget = hoverTarget;
            if (_pressTarget != null)
            {
                _pointer.pressPosition = _pointer.position;
                _pointer.pointerPressRaycast = _pointer.pointerCurrentRaycast;
                GameObject down = ExecuteEvents.ExecuteHierarchy(_pressTarget, _pointer, ExecuteEvents.pointerDownHandler);
                if (down == null) down = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pressTarget);
                _pointer.pointerPress = down;
                _pointer.rawPointerPress = _pressTarget;
            }
        }
        else if (!pressed && _wasPressed)
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

        _wasPressed = pressed;
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
}
