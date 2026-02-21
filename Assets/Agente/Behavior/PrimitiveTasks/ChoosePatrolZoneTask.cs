using AgenticPrison.Core;
using System.Collections.Generic;
using UnityEngine; // Necesario para los Debug.Log

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class ChoosePatrolZoneTask : IPrimitiveTask {
        
        public bool CheckPreconditions(WorldState state) {
            if (state.MapKnowledge == null) {
                Debug.LogError("[HTN-ERROR] ChoosePatrolZone: El mapa es nulo. No puedo planificar.");
                return false;
            }
            if (state.MapKnowledge.GetAllZones().Count == 0) {
                Debug.LogError("[HTN-ERROR] ChoosePatrolZone: El mapa no tiene zonas. ¿Se inicializó bien el MapManager?");
                return false;
            }
            return true; 
        }

        public void ApplyEffects(WorldState state) {
            var allZones = state.MapKnowledge.GetAllZones();
            string bestZoneId = null;
            float bestScore = float.MinValue;

            foreach (var zone in allZones) {
                if (zone.Id == state.CurrentLocationId) continue;

                float score = 0f;
                
                if (!string.IsNullOrEmpty(state.CurrentLocationId)) {
                    float distance = state.MapKnowledge.GetPathDistance(state.CurrentLocationId, zone.Id);
                    if (distance != float.MaxValue) {
                        score -= distance; 
                    }
                }

                if (state.ZoneVisitHistory.TryGetValue(zone.Id, out float lastVisitTime)) {
                    float timeSinceVisit = state.CurrentTime - lastVisitTime;
                    score += timeSinceVisit * 2f; 
                } else {
                    score += 1000f; 
                }

                if (score > bestScore) {
                    bestScore = score;
                    bestZoneId = zone.Id;
                }
            }
            
            state.TargetPatrolZoneId = bestZoneId;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            // ¡ESTO FALTABA! En la vida real, el agente tiene que guardar la decisión en su cerebro
            ApplyEffects(state); 
            Debug.Log($"[HTN-EJECUCIÓN] El guardia ha decidido físicamente patrullar la zona: {state.TargetPatrolZoneId}");
            return TaskExecutionStatus.Success;
        }
    }
}