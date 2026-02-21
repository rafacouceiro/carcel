using System.Collections.Generic;
using AgenticPrison.Core.Math;

namespace AgenticPrison.Core {

    public struct NoiseEvent {
        public Position3D Origin;
        public float Timestamp;
    }

    public struct AgentPerception {
        public Position3D Location;
        public float Timestamp;
    }

    public class WorldState {

        // Visual
        public bool FugitiveInVision;
        public Position3D? LKP;

        // Sound
        public List<NoiseEvent> DetectedNoises = new List<NoiseEvent>();

        // Other Agents
        public Dictionary<int, AgentPerception> OtherAgentsMap = new Dictionary<int, AgentPerception>();

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal State
        public float Alertness = 0f;
        public float Fatigue = 0f;
        
        // Execution Context 
        public float TimeDeltaContext = 0f;
        public float CurrentTime = 0f; // NUEVO: Reloj global

        // Navigation & Memory
        public string CurrentLocationId = string.Empty;
        public string TargetPatrolZoneId = string.Empty; // NUEVO: Zona decidida
        public Dictionary<string, float> ZoneVisitHistory = new Dictionary<string, float>(); // NUEVO: Historial

        // Spatial Knowledge
        public IMapProvider MapKnowledge; // NUEVO: Proveedor de mapa

        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                LKP = this.LKP,
                PrisonerInCell = this.PrisonerInCell,
                Alertness = this.Alertness,
                Fatigue = this.Fatigue,
                CurrentLocationId = this.CurrentLocationId,
                TimeDeltaContext = this.TimeDeltaContext,
                CurrentTime = this.CurrentTime,
                TargetPatrolZoneId = this.TargetPatrolZoneId,
                MapKnowledge = this.MapKnowledge 
            };

            clone.DetectedNoises.AddRange(this.DetectedNoises);
            
            foreach (var kvp in this.OtherAgentsMap) {
                clone.OtherAgentsMap[kvp.Key] = kvp.Value;
            }
            
            foreach (var kvp in this.ZoneVisitHistory) {
                clone.ZoneVisitHistory[kvp.Key] = kvp.Value;
            }

            return clone;
        }
    }
}