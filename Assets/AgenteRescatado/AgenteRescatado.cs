using UnityEngine;

public class AttachRescueObject : MonoBehaviour
{
    [SerializeField] private Transform anchor;     // RescueAnchor del jugador
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    private bool attached = false;

    public void AttachTo(Transform anchorTransform)
    {
        if (attached) return;

        anchor = anchorTransform;
        if (anchor == null)
        {
            Debug.LogError("AttachRescueObject: anchor is null.");
            return;
        }

        // Mantener posición mundial al cambiar de padre
        transform.SetParent(anchor, true);

        // Ahora fijamos offset local exacto
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.Euler(localEulerOffset);

        attached = true;
    }
}