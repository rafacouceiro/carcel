using UnityEngine;

public class PuzzleProximityButton : MonoBehaviour
{
    [Header("ID del botón (0..3)")]
    [Range(0, 3)]
    public int buttonIndex = 0;

    [Header("Distancia para encender")]
    public float activateDistance = 0.8f;

    [Header("Visual")]
    public Light indicatorLight;          // opcional (tu Point Light)
    public Renderer buttonRenderer;       // opcional (mesh del botón)
    public Color offColor = Color.black;  // apagado (puede ser negro)
    public Color onColor = Color.green;   // encendido (verde o el que quieras)
    public float onIntensity = 200f;      // intensidad del Light cuando está ON
    public float onEmission = 4f;         // emisión del material cuando está ON

    [Header("Referencias")]
    public Transform player;              // arrástralo o lo busca por Tag

    private Material _matInstance;
    private bool _isOn = false;

    public bool IsOn => _isOn;

    void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (indicatorLight == null)
            indicatorLight = GetComponentInChildren<Light>();

        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
            _matInstance = buttonRenderer.material;

        SetState(false);
    }

    void Update()
    {
        if (player == null) return;

        // Si ya está ON, no hace falta recalcular
        if (_isOn) return;

        // Distancia real al collider del player (más fiable)
        Collider playerCol = player.GetComponent<Collider>();
        float distance = (playerCol != null)
            ? Vector3.Distance(transform.position, playerCol.ClosestPoint(transform.position))
            : Vector3.Distance(transform.position, player.position);

        if (distance <= activateDistance)
        {
            SetState(true);
            // Avisamos al controlador si existe
            PuzzleCombinationController.NotifyButtonChanged(this);
        }
    }

    public void ForceOff()
    {
        if (_isOn)
        {
            SetState(false);
            PuzzleCombinationController.NotifyButtonChanged(this);
        }
        else
        {
            SetState(false);
        }
    }

    private void SetState(bool on)
    {
        _isOn = on;

        if (indicatorLight != null)
        {
            indicatorLight.color = on ? onColor : offColor;
            indicatorLight.intensity = on ? onIntensity : 0f;
        }

        if (_matInstance != null)
        {
            _matInstance.EnableKeyword("_EMISSION");
            _matInstance.SetColor("_EmissionColor", (on ? onColor : offColor) * (on ? onEmission : 0f));
        }
    }
}