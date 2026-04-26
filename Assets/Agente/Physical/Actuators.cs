using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;

namespace AgenticPrison.Physical {
    
    // Componente puente entre la lógica HTN y los componentes físicos de Unity
    [RequireComponent(typeof(NavMeshAgent))]
    public class Actuators : MonoBehaviour, IActuators {
        private NavMeshAgent _agent;

        [Header("Efectos Visuales")]
        public Light linterna;

        private void Awake() {
            _agent = GetComponent<NavMeshAgent>();
            if (gameObject.CompareTag("Guardia")) {
                _agent.avoidancePriority = Random.Range(30, 70);
            }
        }

        // --- Implementación del control de movimiento ---
        public void SetDestination(Vector3 position) {
            if (_agent.isOnNavMesh) {
                _agent.isStopped = false;
                _agent.SetDestination(position);
            }
        }

        public void SetDestination(Transform target) {
            if (target != null) SetDestination(target.position);
        }   

        public void StopMoving() {
            if (_agent.isOnNavMesh) {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        // Comprueba si el agente sigue desplazándose físicamente hacia su meta
        public bool IsMoving() {
            if (!_agent.isOnNavMesh) return false;
            if (!_agent.pathPending) {
                if (_agent.remainingDistance <= _agent.stoppingDistance) {
                    if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) {
                        return false;
                    }
                }
            }
            return true;
        }

        // Ajusta la velocidad del navmesh
        public void SetSpeed(float speed) {
            _agent.speed = speed;
        }

        // Obtiene el ángulo Y de rotación
        public float GetRotation() => transform.eulerAngles.y;

        // Rota instantáneamente el modelo
        public void RotateTo(float degrees) {
            transform.rotation = Quaternion.Euler(0, degrees, 0);
        }

        // --- Implementación de luces o señales ---
        public void SetLightColor(Color color) {
            if (linterna != null) {
                linterna.color = color;
            }
        }
    }
}