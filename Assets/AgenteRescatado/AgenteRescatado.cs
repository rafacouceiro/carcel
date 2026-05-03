using UnityEngine;

public class AttachRescueObject : MonoBehaviour
{
    [SerializeField] private Transform anchor;         // RescueAnchor del jugador
    [SerializeField] private Vector3 localOffset = Vector3.zero;        // Desplazamiento local respecto al anchor
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;   // Rotación local respecto al anchor

    private bool attached = false; // Estado actual de acoplamiento

    public void AttachTo(Transform anchorTransform)
    {
        // Evitar múltiples ejecuciones si ya está acoplado
        if (attached) return;

        anchor = anchorTransform;
        if (anchor == null)
        {
            Debug.LogError("AttachRescueObject: anchor is null.");
            return;
        }

        // Emparentar con el anchor manteniendo su posición mundial actual
        transform.SetParent(anchor, true);

        // Aplicar la posición y rotación local definidas por el offset
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.Euler(localEulerOffset);

        attached = true; // Marcar como objeto acoplado
    }
}