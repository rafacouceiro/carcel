using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Physical {

    // Datos del evento
    public struct VisionEvent {
        public Transform Source;
        public Vector3 Position;

        public VisionEvent(Transform source, Vector3 pos) {
            Source = source;
            Position = pos;
        }
    }

    // Interfaz para que el Brain reciba señales
    public interface IVisionEvents {
        void OnFugitiveSpotted(Vector3 position);
        void OnFugitiveLost();
        void OnFugitivePositionUpdated(Vector3 position);
    }

    // Evento especial para que el guardia pueda 'ver' la celda abierta
    public interface ICellEventReceiver {
        void OnCellFoundOpen();
    }

    // El mánager que conecta al jugador con los sensores
    public static class VisionManager {
        private static List<VisionSystem> _sensors = new List<VisionSystem>();

        public static void RegisterSensor(VisionSystem sensor) => _sensors.Add(sensor);
        public static void UnregisterSensor(VisionSystem sensor) => _sensors.Remove(sensor);

        public static void EmitPresence(Transform player) {
            foreach (var sensor in _sensors) {
                sensor.OnPlayerPresenceEmitted(player);
            }
        }
    }

    // El sensor físico que va en cada guardia
    public class VisionSystem : MonoBehaviour {
        [Header("Configuración")]
        public float VisionRange = 15f;
        public float ViewAngle = 120f;
        public LayerMask ObstacleMask;

        private IVisionEvents _brain;
        private bool _isCurrentlySeeing = false;

        private void Awake() {
            // Buscamos el Brain en el mismo objeto
            _brain = GetComponent<IVisionEvents>();
        }

        private void OnEnable() => VisionManager.RegisterSensor(this);
        private void OnDisable() => VisionManager.UnregisterSensor(this);

        public void OnPlayerPresenceEmitted(Transform player) {
            bool canSeeNow = CheckPhysicalVisibility(player);

            if (canSeeNow) {
                if (!_isCurrentlySeeing) {
                    // Primer frame: El guardia se sorprende y replanifica
                    _isCurrentlySeeing = true;
                    _brain?.OnFugitiveSpotted(player.position);
                } else {
                    // Siguientes frames: El guardia solo actualiza la coordenada de su objetivo
                    _brain?.OnFugitivePositionUpdated(player.position);
                }
            } 
            else if (!canSeeNow && _isCurrentlySeeing) {
                _isCurrentlySeeing = false;
                _brain?.OnFugitiveLost();
            }
        }

        private bool CheckPhysicalVisibility(Transform target) {
            // 1. Elevamos ambos puntos para que el rayo viaje en línea recta y paralela al suelo
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = target.position + Vector3.up; 
            
            float distToTarget = Vector3.Distance(origin, targetPos);
            Vector3 dirToTarget = (targetPos - origin).normalized;

            // Fuera de rango de visión
            if (distToTarget > VisionRange) return false;

            // 2. Comprobar ángulo (usamos posiciones originales planas para que la altura no distorsione el cono)
            Vector3 flatDir = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, flatDir) > ViewAngle / 2) return false;

            // 3. Raycast: Lanzamos el rayo SOLO hasta la distancia a la que está el jugador
            if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, distToTarget, ObstacleMask)) {
                // Si chocó con algo ANTES de llegar a ti, y ese algo no eres tú, entonces es un muro real
                if (hit.transform != target) {
                    return false;
                }
            }
            return true;
        }
    }
}