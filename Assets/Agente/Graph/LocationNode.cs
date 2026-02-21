using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class LocationNode : MonoBehaviour
{
    [Header("Configuración de Zona")]
    public bool isExit = false;

    [Header("Patrulla")]
    [Range(1, 10)]
    public int pointsToGenerate = 4;
    [SerializeField] private List<Vector3> patrolPoints = new List<Vector3>();

    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
        col.isTrigger = true; 
        GenerateInternalWaypoints();
    }

    public void GenerateInternalWaypoints()
    {
        patrolPoints.Clear();
        Bounds bounds = col.bounds;
        int intentosMaximos = pointsToGenerate * 10;
        int intentosRealizados = 0;

        while (patrolPoints.Count < pointsToGenerate && intentosRealizados < intentosMaximos)
        {
            intentosRealizados++;
            Vector3 puntoAleatorio = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (NavMesh.SamplePosition(puntoAleatorio, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                if (col.ClosestPoint(hit.position) == hit.position)
                {
                    if (!patrolPoints.Contains(hit.position))
                    {
                        patrolPoints.Add(hit.position);
                    }
                }
            }
        }
    }

    // Método expuesto para el MapManager
    public List<Vector3> GetGeneratedPoints() {
        return patrolPoints;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null) return;

        Color colorBase = isExit ? Color.red : Color.green;
        
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(colorBase.r, colorBase.g, colorBase.b, 0.2f);
        Gizmos.DrawCube(collider.center, collider.size);
        Gizmos.color = colorBase;
        Gizmos.DrawWireCube(collider.center, collider.size);

        if (Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix for spheres
            Gizmos.color = Color.yellow;
            foreach (Vector3 p in patrolPoints)
            {
                Gizmos.DrawSphere(p, 0.3f);
            }
        }
    }
    #endif
}