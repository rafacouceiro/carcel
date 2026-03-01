using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;

namespace AgenticPrison.Physical {
    
    [RequireComponent(typeof(NavMeshAgent))]
    public class Actuators : MonoBehaviour, IActuators {
        private NavMeshAgent _agent;

        [Header("Visuales")]
        public Light linterna;

        private void Awake() {
            _agent = GetComponent<NavMeshAgent>();
        }

        // --- Implementación de IMovable ---
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
            transform.rotation = Quaternion.Euler(0, degrees, 0);
        }

        // --- Implementación de ILightActuator ---
        public void SetLightColor(Color color) {
            if (linterna != null) {
                linterna.color = color;
            }
        }
    }
}