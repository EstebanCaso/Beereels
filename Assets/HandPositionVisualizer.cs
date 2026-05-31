using UnityEngine;

public class HandPositionVisualizer : MonoBehaviour
{
    [Header("Arrastra desde la Hierarchy")]
    public OVRHand rightHand;
    public OVRSkeleton rightSkeleton;

    [Header("Estos cubos seguirán la posición de la mano")]
    public Transform handMarker;      // cubo que sigue al OVRHand.transform
    public Transform skeletonMarker;  // cubo que sigue al bone de la muñeca

    void Update()
    {
        // Marker 1 — posición del OVRHand transform directamente
        if (rightHand != null && handMarker != null)
            handMarker.position = rightHand.transform.position;

        // Marker 2 — posición del bone de la muñeca
        if (rightSkeleton != null && skeletonMarker != null &&
            rightSkeleton.Bones != null && rightSkeleton.Bones.Count > 0)
        {
            var bone = rightSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot];
            if (bone?.Transform != null)
                skeletonMarker.position = bone.Transform.position;
        }
    }
}