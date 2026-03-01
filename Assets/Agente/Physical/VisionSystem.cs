using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Physical {

    // Información transmitida en los avistamientos visuales
    public struct VisionEvent {
        public Transform Source;
        public Vector3 Position;

        public VisionEvent(Transform source, Vector3 pos) {
            Source = source;
            Position = pos;
        }
    }

    // Eventos disparados cuando un guardia percibe entidades
    public interface IVisionEvents {
        void OnFugitiveSpotted(Vector3 position);
        void OnFugitiveLost();
        void OnFugitivePositionUpdated(Vector3 position);
        void OnGuardSpotted(Vector3 guardPosition);
    }

    // Interfaz adicional para notificar encuentros relacionados con celdas
    public interface ICellEventReceiver {
        void OnCellFoundOpen();
    }

    // Administrador para registrar qué entidades están activamente mirando
    public static class VisionManager {
        private static List<VisionSystem> _sensors = new List<VisionSystem>();

        public static void RegisterSensor(VisionSystem sensor) => _sensors.Add(sensor);
        public static void UnregisterSensor(VisionSystem sensor) => _sensors.Remove(sensor);

        // Hace que las entidades (jugador u otro guardia) anuncien su presencia a los observadores
        public static void EmitPresence(Transform entity) {
            foreach (var sensor in _sensors) {
                sensor.OnPresenceEmitted(entity); 
            }
        }
    }

    // Componente sensor acoplado al agente para la visión en cono
    public class VisionSystem : MonoBehaviour {
        [Header("Configuración del Sensor")]
        public float VisionRange = 30f;
        public float ViewAngle = 140f;
        public LayerMask ObstacleMask;

        private IVisionEvents _brain;
        private bool _isCurrentlySeeingPlayer = false; // Indica si el usuario está fijado visualmente

        private void Awake() {
            _brain = GetComponent<IVisionEvents>();
        }

        private void OnEnable() => VisionManager.RegisterSensor(this);
        private void OnDisable() => VisionManager.UnregisterSensor(this);

        // --- Análisis de Detección ---
        public void OnPresenceEmitted(Transform target) {
            
            // Un guardia ignora su propia emisión
            if (target == this.transform) return;

            bool canSeeNow = CheckPhysicalVisibility(target);

            // Reacciones ante la meta (El Presidiario)
            if (target.CompareTag("Player")) {
                if (canSeeNow) {
                    if (!_isCurrentlySeeingPlayer) {
                        _isCurrentlySeeingPlayer = true;
                        _brain?.OnFugitiveSpotted(target.position);
                    } else {
                        // Actualizar traza constante
                        _brain?.OnFugitivePositionUpdated(target.position);
                    }
                } 
                else if (!canSeeNow && _isCurrentlySeeingPlayer) {
                    _isCurrentlySeeingPlayer = false;
                    _brain?.OnFugitiveLost();
                }
            }
            // Reacciones ante otro vigilante (Comunicación implícita/Coordinación)
            else if (target.CompareTag("Guardia")) {
                if (canSeeNow) {
                    _brain?.OnGuardSpotted(target.position);
                }
            }
        }

        // Utiliza físicas para trazar una línea de visión considerando muros y ángulo
        private bool CheckPhysicalVisibility(Transform target) {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = target.position + Vector3.up; 
            
            float distToTarget = Vector3.Distance(origin, targetPos);
            Vector3 dirToTarget = (targetPos - origin).normalized;

            // Escapó del rango de profundidad
            if (distToTarget > VisionRange) return false;

            // Revisión de visión periférica
            Vector3 flatDir = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, flatDir) > ViewAngle / 2) return false;

            // Confirmación final revisando las paredes usando Raycast
            if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, distToTarget, ObstacleMask)) {
                if (hit.transform != target) {
                    return false;
                }
            }
            return true;
        }
    }
}