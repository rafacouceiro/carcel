using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core.Math;
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

        public void SetDestination(Position3D target) {
            _agent.isStopped = false;
            _agent.SetDestination(new Vector3(target.X, target.Y, target.Z));
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
