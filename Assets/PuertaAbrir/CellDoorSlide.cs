using UnityEngine;

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
    }

    public void Open() => _isOpen = true;
    public void Close() => _isOpen = false;
}