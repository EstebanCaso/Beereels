using UnityEngine;

public class HandPointerManager : MonoBehaviour
{
    public static HandPointerManager Instance;
    public OVRHandPointer leftPointer;
    public OVRHandPointer rightPointer;

    void Awake()
    {
        Instance = this;
        SetPointersActive(false); // ocultos al inicio
    }

    public void SetPointersActive(bool active)
    {
        if (leftPointer) leftPointer.SetActive(active);
        if (rightPointer) rightPointer.SetActive(active);
    }
}