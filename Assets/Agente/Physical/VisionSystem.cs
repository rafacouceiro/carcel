using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Physical {

    public struct VisionEvent {
        public Transform Source;
        public Vector3 Position;

        public VisionEvent(Transform source, Vector3 pos) {
            Source = source;
            Position = pos;
        }
    }

    public interface IVisionEvents {
        void OnFugitiveSpotted(Vector3 position);
        void OnFugitiveLost();
        void OnFugitivePositionUpdated(Vector3 position);
        void OnGuardSpotted(Vector3 guardPosition);
    }

    public interface ICellEventReceiver {
        void OnCellFoundOpen();
    }

    public static class VisionManager {
        private static List<VisionSystem> _sensors = new List<VisionSystem>();

        public static void RegisterSensor(VisionSystem sensor) => _sensors.Add(sensor);
        public static void UnregisterSensor(VisionSystem sensor) => _sensors.Remove(sensor);

        // Renombrado a 'entity' porque ahora puede ser jugador o guardia
        public static void EmitPresence(Transform entity) {
            foreach (var sensor in _sensors) {
                sensor.OnPresenceEmitted(entity); 
            }
        }
    }

    public class VisionSystem : MonoBehaviour {
        [Header("Configuración")]
        public float VisionRange = 30f;
        public float ViewAngle = 140f;
        public LayerMask ObstacleMask;

        private IVisionEvents _brain;
        private bool _isCurrentlySeeingPlayer = false; // Renombrado para claridad

        private void Awake() {
            _brain = GetComponent<IVisionEvents>();
        }

        private void OnEnable() => VisionManager.RegisterSensor(this);
        private void OnDisable() => VisionManager.UnregisterSensor(this);

        // --- 2. LÓGICA DE DETECCIÓN DIFERENCIADA ---
        public void OnPresenceEmitted(Transform target) {
            
            // Un guardia no debe intentar verse a sí mismo
            if (target == this.transform) return;

            bool canSeeNow = CheckPhysicalVisibility(target);

            // Si es el JUGADOR (El Preso)
            if (target.CompareTag("Player")) {
                if (canSeeNow) {
                    if (!_isCurrentlySeeingPlayer) {
                        _isCurrentlySeeingPlayer = true;
                        _brain?.OnFugitiveSpotted(target.position);
                    } else {
                        _brain?.OnFugitivePositionUpdated(target.position);
                    }
                } 
                else if (!canSeeNow && _isCurrentlySeeingPlayer) {
                    _isCurrentlySeeingPlayer = false;
                    _brain?.OnFugitiveLost();
                }
            }
            // Si es UN COMPAÑERO GUARDIA
            else if (target.CompareTag("Guardia")) {
                if (canSeeNow) {
                    _brain?.OnGuardSpotted(target.position);
                }
            }
        }

        private bool CheckPhysicalVisibility(Transform target) {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = target.position + Vector3.up; 
            
            float distToTarget = Vector3.Distance(origin, targetPos);
            Vector3 dirToTarget = (targetPos - origin).normalized;

            if (distToTarget > VisionRange) return false;

            Vector3 flatDir = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, flatDir) > ViewAngle / 2) return false;

            if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, distToTarget, ObstacleMask)) {
                if (hit.transform != target) {
                    return false;
                }
            }
            return true;
        }
    }
}