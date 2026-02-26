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
            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > VisionRange) return false;
            if (Vector3.Angle(transform.forward, dir) > ViewAngle / 2) return false;

            // Raycast para evitar ver a través de muros
            if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, VisionRange, ObstacleMask)) {
                if (hit.transform != target) return false;
            }
            return true;
        }
    }
}