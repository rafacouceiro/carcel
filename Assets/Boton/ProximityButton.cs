using UnityEngine;
using AgenticPrison.Physical;

public class ProximityButton : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player; 

    [Header("Puertas a abrir")]
    [SerializeField] private CellDoorSlide[] doorsToOpen;

    [Header("Objeto a rescatar")]
    [SerializeField] private AttachRescueObject rescueObject;
    [SerializeField] private Transform rescueAnchor;

    [Header("Distancia para activar")]
    [SerializeField] private float activateDistance = 1.2f;

    [Header("Visual")]
    [SerializeField] private Light indicatorLight;
    [SerializeField] private Renderer buttonRenderer;

    [Header("Colores")]
    [SerializeField] private Color idleColor = Color.red;
    [SerializeField] private Color activatedColor = Color.green;

    [Header("Parpadeo")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float maxIntensity = 200f;

    private Material matInstance;
    private bool activated = false;

    void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (indicatorLight == null)
            indicatorLight = GetComponentInChildren<Light>();
        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();
        if (buttonRenderer != null)
            matInstance = buttonRenderer.material;
    }

    void Update()
    {
        if (player == null || activated) return;

        float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);

        if (indicatorLight != null) {
            indicatorLight.color = idleColor;
            indicatorLight.intensity = Mathf.Lerp(0f, maxIntensity, t);
        }

        if (matInstance != null) {
            matInstance.EnableKeyword("_EMISSION");
            matInstance.SetColor("_EmissionColor", idleColor * Mathf.Lerp(0f, 3f, t));
        }

        Collider playerCol = player.GetComponent<Collider>();
        float distance = (playerCol != null)
            ? Vector3.Distance(transform.position, playerCol.ClosestPoint(transform.position))
            : Vector3.Distance(transform.position, player.position);

        if (distance <= activateDistance)
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chivato 1: Ver si las físicas están chocando
        Debug.Log($"<color=cyan>[Trigger Botón] Algo ha tocado la zona invisible: {other.gameObject.name}</color>");

        if (!activated) return;

        var brain = other.GetComponentInParent<ICellEventReceiver>();
        if (brain != null)
        {
            Debug.Log($"<color=green>[Trigger Botón] ¡Cerebro encontrado en {other.gameObject.name}! Avisando...</color>");
            brain.OnCellFoundOpen();
        }
    }

    private void Activate()
    {
        activated = true;

        if (indicatorLight != null) {
            indicatorLight.color = activatedColor;
            indicatorLight.intensity = maxIntensity;
        }

        if (matInstance != null) {
            matInstance.EnableKeyword("_EMISSION");
            matInstance.SetColor("_EmissionColor", activatedColor * 4f);
        }

        if (doorsToOpen != null && doorsToOpen.Length > 0) {
            foreach (var door in doorsToOpen) {
                if (door != null) door.Open();
            }
        }

        if (rescueObject != null) {
            if (rescueAnchor == null && player != null)
                rescueAnchor = player.Find("RescueAnchor");

            if (rescueAnchor != null)
                rescueObject.AttachTo(rescueAnchor);
        }

        EscapeState.CanEscape = true; 

        // --- CÁLCULO DE RADIO MUNDIAL REAL ---
        SphereCollider myCollider = GetComponent<SphereCollider>();
        float alertRadius = 25f;
        if (myCollider != null) {
            // Multiplicamos el radio por la escala más grande del objeto para obtener metros reales en Unity
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            alertRadius = myCollider.radius * maxScale;
        }

        Debug.Log($"<color=magenta>[Botón] Celda Abierta. Lanzando radar con radio MUNDIAL: {alertRadius} metros.</color>");

        Collider[] guards = Physics.OverlapSphere(transform.position, alertRadius);
        Debug.Log($"<color=magenta>[Botón] El radar ha chocado con {guards.Length} objetos.</color>");

        foreach (var g in guards) {
            var brain = g.GetComponentInParent<ICellEventReceiver>();
            if (brain != null) {
                Debug.Log($"<color=green>[Botón] He encontrado un cerebro en {g.gameObject.name} mediante radar. Avisando...</color>");
                brain.OnCellFoundOpen();
            }
        }
    }
}