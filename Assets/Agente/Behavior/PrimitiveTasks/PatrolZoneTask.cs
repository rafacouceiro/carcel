using AgenticPrison.Core;
using AgenticPrison.Core.Math;
using System.Collections.Generic;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class PatrolZoneTask : IPrimitiveTask {
        
        private Queue<Position3D> _waypoints;
        private bool _isInitialized = false;

        public bool CheckPreconditions(WorldState state) {
            // Para patrullar una zona, primero debemos haber elegido una
            return !string.IsNullOrEmpty(state.TargetPatrolZoneId);
        }

        public void ApplyEffects(WorldState state) {
            // Simulamos que al final de esta tarea, habremos visitado la zona
            state.CurrentLocationId = state.TargetPatrolZoneId;
            state.ZoneVisitHistory[state.TargetPatrolZoneId] = state.CurrentTime;
            
            // Limpiamos el objetivo para la próxima vez
            state.TargetPatrolZoneId = null; 
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            // 1. Inicialización: Cargar los puntos de la zona
            if (!_isInitialized) {
                var zoneData = state.MapKnowledge.GetZone(state.TargetPatrolZoneId);
                
                // Si la zona no tiene puntos generados, usamos su centro
                if (zoneData.PatrolPoints.Count > 0) {
                    _waypoints = new Queue<Position3D>(zoneData.PatrolPoints);
                } else {
                    _waypoints = new Queue<Position3D>();
                    _waypoints.Enqueue(zoneData.Center);
                }
                
                _isInitialized = true;
            }

            // 2. Comprobar si hemos terminado todos los puntos
            if (_waypoints.Count == 0) {
                _isInitialized = false; // Reset para la próxima vez
                return TaskExecutionStatus.Success;
            }

            // 3. Lógica de movimiento
            if (!actuators.IsMoving()) {
                // Si estamos parados (o acabamos de llegar a un punto), vamos al siguiente
                var nextPoint = _waypoints.Dequeue();
                actuators.SetDestination(nextPoint);
            }

            // Seguimos ejecutando la tarea mientras queden puntos o nos estemos moviendo
            return TaskExecutionStatus.Running;
        }
    }
}