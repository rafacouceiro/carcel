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
    [SerializeField] private Transform rescueAnchor; // Empty dentro del Player

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
        if (player == null || activated)
            return;

        // Parpadeo fuerte (0 -> max -> 0)
        float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);

        if (indicatorLight != null)
        {
            indicatorLight.color = idleColor;
            indicatorLight.intensity = Mathf.Lerp(0f, maxIntensity, t);
        }

        if (matInstance != null)
        {
            matInstance.EnableKeyword("_EMISSION");
            matInstance.SetColor("_EmissionColor", idleColor * Mathf.Lerp(0f, 3f, t));
        }

        // Distancia real al collider del jugador
        Collider playerCol = player.GetComponent<Collider>();
        float distance = (playerCol != null)
            ? Vector3.Distance(transform.position, playerCol.ClosestPoint(transform.position))
            : Vector3.Distance(transform.position, player.position);

        if (distance <= activateDistance)
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Si la celda sigue cerrada, no le decimos nada a nadie
        if (!activated) return;

        // 2. Comprobamos si el que acaba de pasar por aquí es un guardia (tiene cerebro)
        var brain = other.GetComponent<ICellEventReceiver>();
        if (brain != null)
        {
            // 3. Le mandamos la señal UNA VEZ al guardia que ha pasado
            brain.OnCellFoundOpen();
        }
    }

    private void Activate()
    {
        activated = true;

        // Verde fijo
        if (indicatorLight != null)
        {
            indicatorLight.color = activatedColor;
            indicatorLight.intensity = maxIntensity;
        }

        if (matInstance != null)
        {
            matInstance.EnableKeyword("_EMISSION");
            matInstance.SetColor("_EmissionColor", activatedColor * 4f);
        }

        // Abrir todas las puertas asignadas
        if (doorsToOpen != null && doorsToOpen.Length > 0)
        {
            foreach (var door in doorsToOpen)
            {
                if (door != null)
                    door.Open();
            }
        }

        // Rescatar objeto y pegarlo al jugador
        if (rescueObject != null)
        {
            if (rescueAnchor == null && player != null)
            {
                rescueAnchor = player.Find("RescueAnchor");
            }

            if (rescueAnchor != null)
            {
                rescueObject.AttachTo(rescueAnchor);
            }
            else
            {
                Debug.LogWarning("No se ha asignado RescueAnchor.");
            }
        }

        EscapeState.CanEscape = true; // Activar la posibilidad de escapar

        Collider[] guards = Physics.OverlapSphere(transform.position, 4f);
        foreach (var g in guards) {
            g.GetComponent<ICellEventReceiver>()?.OnCellFoundOpen();
        }
    
    }
}