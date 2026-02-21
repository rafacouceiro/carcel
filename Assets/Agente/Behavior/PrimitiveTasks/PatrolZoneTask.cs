using AgenticPrison.Core;
using AgenticPrison.Core.Math;
using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class PatrolZoneTask : IPrimitiveTask {
        
        private Queue<Position3D> _waypoints;
        private bool _isInitialized = false;

        public bool CheckPreconditions(WorldState state) {
            bool hasTarget = !string.IsNullOrEmpty(state.TargetPatrolZoneId);
            if (!hasTarget) {
                Debug.LogWarning("[HTN-PRECONDICIÓN] PatrolZoneTask abortada: TargetPatrolZoneId está vacío o nulo.");
            }
            return hasTarget;
        }

        public void ApplyEffects(WorldState state) {
            state.CurrentLocationId = state.TargetPatrolZoneId;
            state.ZoneVisitHistory[state.TargetPatrolZoneId] = state.CurrentTime;
            state.TargetPatrolZoneId = null; 
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            if (!_isInitialized) {
                var zoneData = state.MapKnowledge.GetZone(state.TargetPatrolZoneId);
                
                if (zoneData.PatrolPoints.Count > 0) {
                    _waypoints = new Queue<Position3D>(zoneData.PatrolPoints);
                } else {
                    _waypoints = new Queue<Position3D>();
                    _waypoints.Enqueue(zoneData.Center);
                }
                
                _isInitialized = true;
            }

            if (!actuators.IsMoving()) {
                if (_waypoints.Count == 0) {
                    _isInitialized = false; 
                    // ¡ESTO FALTABA! Aplicamos el efecto real (borrar el objetivo) cuando termina
                    ApplyEffects(state);
                    Debug.Log("[HTN-EJECUCIÓN] Patrulla completada con éxito.");
                    return TaskExecutionStatus.Success;
                }

                var nextPoint = _waypoints.Dequeue();
                actuators.SetDestination(nextPoint);
            }

            return TaskExecutionStatus.Running;
        }
    }
}