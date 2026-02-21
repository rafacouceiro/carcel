using AgenticPrison.Core;
using System.Collections.Generic;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class ChoosePatrolZoneTask : IPrimitiveTask {
        
        public bool CheckPreconditions(WorldState state) {
            // Solo podemos decidir si tenemos un mapa cargado
            return state.MapKnowledge != null; 
        }

        public void ApplyEffects(WorldState state) {
            
            var allZones = state.MapKnowledge.GetAllZones();
            string bestZoneId = null;
            float bestScore = float.MinValue;

            foreach (var zone in allZones) {
                // Evitamos elegir la zona en la que ya estamos para no quedarnos quietos
                if (zone.Id == state.CurrentLocationId) continue;

                float score = 0f;

                // FACTOR 1: Distancia (Penaliza las zonas lejanas)
                // Usamos la distancia del NavMesh que calculamos en el MapManager
                float distance = state.MapKnowledge.GetPathDistance(state.CurrentLocationId, zone.Id);
                score -= distance; // A mayor distancia, menor puntuación

                // FACTOR 2: Tiempo sin visitar (Premia las zonas olvidadas)
                if (state.ZoneVisitHistory.TryGetValue(zone.Id, out float lastVisitTime)) {
                    float timeSinceVisit = state.CurrentTime - lastVisitTime;
                    score += timeSinceVisit * 2f; // Multiplicador para darle peso al aburrimiento
                } else {
                    // Si nunca ha estado ahí, le damos máxima prioridad
                    score += 1000f; 
                }

                // (Opcional) FACTOR 3: Podrías restar puntos si hay compañeros cerca usando state.OtherAgentsMap

                if (score > bestScore) {
                    bestScore = score;
                    bestZoneId = zone.Id;
                }
            }

            // EFECTO: El agente "decide" ir a esta zona
            state.TargetPatrolZoneId = bestZoneId;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            // Como es una tarea puramente mental de toma de decisión, termina instantáneamente
            return TaskExecutionStatus.Success;
        }
    }
}