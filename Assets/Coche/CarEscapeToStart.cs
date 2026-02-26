using UnityEngine;
using UnityEngine.SceneManagement;

public class CarEscapeToStart : MonoBehaviour
{
    [Header("Distancia para escapar")]
    [SerializeField] private float escapeDistance = 0.6f;

    [Header("Escena inicial")]
    [SerializeField] private string startSceneName = "MainMenu"; // cámbialo por el nombre real

    [Header("Referencias")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private AttachRescueObject rescuedObject; // opcional

    private bool done = false;

    void Awake()
    {
        if (playerRoot == null)
            playerRoot = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (done) return;
        if (!EscapeState.CanEscape) return;
        if (playerRoot == null) return;

        // Distancia al collider del jugador (más fiable)
        Collider playerCol = playerRoot.GetComponent<Collider>();
        float d = (playerCol != null)
            ? Vector3.Distance(transform.position, playerCol.ClosestPoint(transform.position))
            : Vector3.Distance(transform.position, playerRoot.position);

        if (d <= escapeDistance)
            Escape();
    }

    private void Escape()
{
    done = true;

    Debug.Log("HAS ESCAPADO. Cerrando juego...");

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private void SetAllRenderersEnabled(GameObject root, bool enabled)
    {
        if (root == null) return;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = enabled;
    }
}