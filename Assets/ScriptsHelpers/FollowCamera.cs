using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [Header("Objetivo a seguir")]
    public Transform target; // Arrastra aquí a tu jugador/agente desde el Inspector

    [Header("Ajustes de Cámara")]
    public float height = 20f; // La altura a la que vuela la cámara
    public float smoothSpeed = 5f; // Para que el seguimiento sea suave

    void LateUpdate() {
        if (target == null) return;

        // 1. Calculamos la posición ideal: la X y Z del jugador, pero manteniendo nuestra Y (altura)
        Vector3 desiredPosition = new Vector3(target.position.x, height, target.position.z);
        
        // 2. Movemos la cámara suavemente hacia esa posición
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}