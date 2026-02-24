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
        public void SetDestination(Transform target) {
            if (target != null) {
                _agent.isStopped = false;
                // Le pasamos al NavMesh la coordenada exacta (Vector3) de ese Transform
                _agent.SetDestination(target.position);
            } else {
                Debug.LogWarning("[NavMeshDriver] Cuidado: Has intentado mandar al agente a un Transform nulo.");
            }
        }

        // (Opcional pero muy recomendado): Dejo esta versión con Vector3 por si 
        // algún día necesitas mandar al agente a un "ruido" que no tiene un Transform físico.
        public void SetDestination(Vector3 targetPosition) {
            _agent.isStopped = false;
            _agent.SetDestination(targetPosition);
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
    }
}