using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;

namespace AgenticPrison.Physical {
    /// <summary>
    /// Wrapper for Unity's NavMeshAgent to expose basic movement functionality
    /// through the pure C# IMovable interface.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshDriver : MonoBehaviour, IMovable {
        private NavMeshAgent _agent;

        private void Awake() {
            _agent = GetComponent<NavMeshAgent>();
        }

        // --- EL CAMBIO ESTÁ AQUÍ ---
        // Ahora recibe un Transform directamente (tu Waypoint)
        public void SetDestination(Vector3 position) {
            _agent.isStopped = false;
            _agent.SetDestination(position);
        }

        // Sobrecarga 2: Recibe un Transform (Ideal para seguir a un fugitivo en movimiento)
        public void SetDestination(Transform target) {
            if (target != null) {
                // Reutilizamos la lógica del Vector3 pasando target.position
                SetDestination(target.position);
            } else {
                Debug.LogWarning("[NavMeshDriver] Cuidado: Intento de ir a un Transform nulo.");
            }
        }   

        public void StopMoving() {
            if (_agent.isOnNavMesh) {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        public bool IsMoving() {
            if (!_agent.pathPending) {
                if (_agent.remainingDistance <= _agent.stoppingDistance) {
                    if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) {
                        return false;
                    }
                }
            }
            return true;
        }

        public void SetSpeed(float speed) {
            _agent.speed = speed;
        }

        public float GetRotation() => transform.eulerAngles.y;

        public void RotateTo(float degrees) {
            // NavMeshAgent suele controlar la rotación, pero para mirar a los lados 
            // mientras está quieto, rotamos el transform directamente.
            // Usamos el ángulo como un offset sobre el eje Y.
            transform.rotation = Quaternion.Euler(0, degrees, 0);
        }
    }
}