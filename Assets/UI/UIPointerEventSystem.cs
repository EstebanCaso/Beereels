using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Garantiza que exista un EventSystem en la escena sin StandaloneInputModule.
/// El input lo emite OVRHandPointer directamente con ExecuteEvents.
/// Pegar este componente en el mismo GameObject que el EventSystem.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class UIPointerEventSystem : MonoBehaviour
{
    void Awake()
    {
        // Quitar cualquier InputModule que Unity haya a�adido por defecto:
        // un StandaloneInputModule activo intentar� leer mouse/teclado y
        // pelear con nuestros eventos sint�ticos de OVRHandPointer.
        BaseInputModule[] modules = GetComponents<BaseInputModule>();
        for (int i = 0; i < modules.Length; i++)
        {
            modules[i].enabled = false;
        }

        EventSystem es = GetComponent<EventSystem>();
        if (EventSystem.current == null) EventSystem.current = es;
    }
}
