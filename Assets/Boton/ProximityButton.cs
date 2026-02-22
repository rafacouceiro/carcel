using UnityEngine;

public class ProximityButton : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private CellDoorSlide doorToOpen;

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

        // 🔴 Parpadeo completo (0 a máximo)
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

        // 📏 Distancia real al collider del jugador
        Collider playerCol = player.GetComponent<Collider>();
        float distance;

        if (playerCol != null)
            distance = Vector3.Distance(transform.position, playerCol.ClosestPoint(transform.position));
        else
            distance = Vector3.Distance(transform.position, player.position);

        if (distance <= activateDistance)
        {
            Activate();
        }
    }

    private void Activate()
    {
        activated = true;

        // 🟢 Se pone verde fijo
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

        if (doorToOpen != null)
            doorToOpen.Open();
        else
            Debug.LogWarning("No has asignado la puerta en doorToOpen.");
    }
}