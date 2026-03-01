using UnityEngine;
using AgenticPrison.Physical; // Necesario para hablar con el NoiseManager

public class CellDoorSlide : MonoBehaviour
{
    [Header("Arrastra aquí la reja (HIJA) que se mueve")]
    [SerializeField] private Transform door;

    [Header("Offset de apertura (LOCAL de la reja)")]
    [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 3f, 0f);

    [SerializeField] private float speed = 4f;

    private Vector3 _closedLocalPos;
    private Vector3 _openLocalPos;
    private bool _isOpen;

    // --- Variables para el Ruido y el Gizmo ---
    private float _debugNoiseTimer = 0f;
    private const float NoiseRadius = 30f; 

    void Awake()
    {
        if (door == null)
        {
            Debug.LogError("CellDoorSlide: No has asignado 'door' (la reja hija).");
            enabled = false;
            return;
        }

        // Importante: posiciones LOCALES de la HIJA
        _closedLocalPos = door.localPosition;
        _openLocalPos = _closedLocalPos + openLocalOffset;
    }

    void Update()
    {
        Vector3 target = _isOpen ? _openLocalPos : _closedLocalPos;
        door.localPosition = Vector3.Lerp(door.localPosition, target, Time.deltaTime * speed);

        // --- Reducir temporizador del Gizmo visual ---
        if (_debugNoiseTimer > 0f) {
            _debugNoiseTimer -= Time.deltaTime;
        }
    }

    public void Open() 
    {
        // El 'if' evita que si el botón se pulsa 3 veces seguidas, la puerta suelte 3 ruidos de golpe
        if (!_isOpen) 
        {
            _isOpen = true;

            // 1. Emitir el ruido al sistema. Pasamos su propio Transform como identidad.
            NoiseManager.EmitNoise(new NoiseEvent(transform.position, NoiseRadius, "cell_door"));

            // 2. Encender el Gizmo durante medio segundo para poder depurarlo visualmente
            _debugNoiseTimer = 0.5f;
        }
    }

    public void Close() => _isOpen = false;

    // --- DIBUJO DEL RUIDO EN EL EDITOR ---
    private void OnDrawGizmos() 
    {
        if (_debugNoiseTimer > 0f) 
        {
            // Usamos un color naranja/rojo para que destaque sobre el cyan de los guardias
            Color gizmoColor = new Color(1f, 0.4f, 0f); 
            
            gizmoColor.a = 0.2f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, NoiseRadius);

            gizmoColor.a = 1f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, NoiseRadius);
        }
    }
}