using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class LocationNode : MonoBehaviour
{
    [Header("Configuración de Zona")]
    public string zoneName = "Nueva Zona";
    public bool isExit = false; // Marcar si esta zona es una salida de la cárcel

    [Header("Patrulla")]
    [Range(1, 10)]
    public int pointsToGenerate = 4;
    [SerializeField] private List<Vector3> patrolPoints = new List<Vector3>();

    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
        col.isTrigger = true; // Aseguramos que no colisione físicamente
        GenerateInternalWaypoints();
    }

    /// <summary>
    /// Genera puntos aleatorios dentro del BoxCollider que toquen el suelo (NavMesh).
    /// </summary>
    public void GenerateInternalWaypoints()
    {
        patrolPoints.Clear();
        Bounds bounds = col.bounds;

        int attempts = 0;
        while (patrolPoints.Count < pointsToGenerate && attempts < 50)
        {
            attempts++;

            // 1. Punto aleatorio dentro del cubo
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y, // Usamos la altura del objeto como base
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // 2. Proyectar ese punto al NavMesh más cercano
            // El radio de 2.0f es para asegurar que encuentre suelo si el punto cae sobre un obstáculo
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                if (!patrolPoints.Contains(hit.position))
                {
                    patrolPoints.Add(hit.position);
                }
            }
        }
    }

    /// <summary>
    /// Devuelve un punto de patrulla aleatorio de esta zona.
    /// </summary>
    public Vector3 GetRandomPatrolPoint()
    {
        if (patrolPoints.Count == 0) return transform.position;
        return patrolPoints[Random.Range(0, patrolPoints.Count)];
    }

    // --- AYUDAS VISUALES PARA EL EDITOR ---

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null) return;

        // Color verde para zonas normales, rojo para salidas
        Color colorBase = isExit ? Color.red : Color.green;
        
        // Dibujar el cubo transparente
        Gizmos.color = new Color(colorBase.r, colorBase.g, colorBase.b, 0.2f);
        Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);

        // Dibujar el borde del cubo
        Gizmos.color = colorBase;
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);

        // Dibujar los puntos de patrulla generados (solo si estamos en Play Mode)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 p in patrolPoints)
            {
                Gizmos.DrawSphere(p, 0.3f);
            }
        }
    }
    #endif
}