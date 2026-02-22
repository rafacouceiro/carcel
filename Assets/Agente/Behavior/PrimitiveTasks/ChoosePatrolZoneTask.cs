using AgenticPrison.Core;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class ChoosePatrolZoneTask : IPrimitiveTask {
        
        private readonly int _topNCandidates = 7 ; 

        public bool CheckPreconditions(WorldState state) {
            // ARREGLO 1: Ya no exigimos que sepa dónde está. Solo exigimos que el mapa exista.
            return state.MapKnowledge != null && state.MapKnowledge.GetAllZones().Count > 0;
        }

        public void ApplyEffects(WorldState state) {
            var allZones = state.MapKnowledge.GetAllZones();
            
            // --- ARREGLO 2: EL INSTINTO (Si acaba de spawnear y no sabe dónde está) ---
            if (string.IsNullOrEmpty(state.CurrentLocationId)) {
                // Elige cualquier zona aleatoria para echar a andar y "ubicarse"
                state.TargetPatrolZoneId = allZones[UnityEngine.Random.Range(0, allZones.Count)].Id;
                return;
            }

            var zonesWithDistance = new List<(ZoneData zone, float distance)>();

            foreach (var zone in allZones) {
                if (zone.Id == state.CurrentLocationId) continue;

                float dist = state.MapKnowledge.GetPathDistance(state.CurrentLocationId, zone.Id);
                // Si el NavMesh pudo calcular la ruta (no es infinito), la añadimos
                if (dist != float.MaxValue) {
                    zonesWithDistance.Add((zone, dist));
                }
            }

            // --- ARREGLO 3: ANTI-ATASCOS NAVMESH ---
            // Si por algún motivo físico no puede llegar a NINGUNA otra zona, pillamos una cualquiera
            if (zonesWithDistance.Count == 0) {
                var fallbackZone = allZones.FirstOrDefault(z => z.Id != state.CurrentLocationId);
                if (fallbackZone != null) state.TargetPatrolZoneId = fallbackZone.Id;
                return;
            }

            // FILTRO TOP N
            var topZones = zonesWithDistance.OrderBy(z => z.distance).Take(_topNCandidates).ToList();

            var scoredZones = new List<(ZoneData zone, float score)>();
            float totalScore = 0f;

            foreach (var item in topZones) {
                var zone = item.zone;
                float dist = item.distance;
                float baseScore = 100f; 

                if (state.ZoneVisitHistory.TryGetValue(zone.Id, out float lastVisitTime)) {
                    float timeSinceVisit = state.CurrentTime - lastVisitTime;
                    
                    if (timeSinceVisit < 90f) {
                        baseScore *= 0.01f; 
                    } else {
                        baseScore += (timeSinceVisit * 0.5f); 
                    }
                } else {
                    baseScore += 500f; 
                }

                float multiplicadorDistancia = 10f / Mathf.Max(1f, dist);
                baseScore *= multiplicadorDistancia;

                baseScore = Mathf.Max(1f, baseScore);
                scoredZones.Add((zone, baseScore));
                totalScore += baseScore;
            }

            // RULETA (Aseguramos que Random.Range use floats poniendo 0f)
            float randomPoint = UnityEngine.Random.Range(0f, totalScore);
            string selectedZoneId = topZones[0].zone.Id; 

            foreach (var sz in scoredZones) {
                randomPoint -= sz.score;
                if (randomPoint <= 0) {
                    selectedZoneId = sz.zone.Id;
                    break;
                }
            }

            state.TargetPatrolZoneId = selectedZoneId;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            actuators.SetSpeed(2.5f);
            ApplyEffects(state);
            Debug.Log($"[TÁCTICA] Evaluados los {_topNCandidates} más cercanos. Seleccionada la zona: {state.TargetPatrolZoneId}");
            return TaskExecutionStatus.Success;
        }
    }
}