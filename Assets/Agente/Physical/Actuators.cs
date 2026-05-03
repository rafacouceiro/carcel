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

        // Contador estático para asignar prioridades únicas y espaciadas a cada guardia
        private static int _priorityCounter = 0;

        private void Awake() {
            _agent = GetComponent<NavMeshAgent>();
            if (gameObject.CompareTag("Guardia")) {
                // Prioridades 10, 20, 30... evitan empates que causan bloqueos en pasillos
                _agent.avoidancePriority = 10 + (_priorityCounter++ % 9) * 10;
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

        // Tiempo que lleva el agente sin moverse cuando debería hacerlo
        private float _stuckTimer = 0f;
        private const float StuckTimeout = 2.5f;

        // Comprueba si el agente sigue desplazándose físicamente hacia su meta
        public bool IsMoving() {
            if (!_agent.isOnNavMesh) return false;
            if (!_agent.pathPending) {
                if (_agent.remainingDistance <= _agent.stoppingDistance) {
                    if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) {
                        _stuckTimer = 0f;
                        return false;
                    }
                }
            }

            // Si hay destino pero la velocidad es cero, el agente está atascado
            if (_agent.hasPath && _agent.velocity.sqrMagnitude < 0.01f) {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= StuckTimeout) {
                    _stuckTimer = 0f;
                    return false; // Dar la tarea por terminada y replanificar
                }
            } else {
                _stuckTimer = 0f;
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