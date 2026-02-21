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

    /// <summary>
    /// Pure C# data structure representing the agent's current "mental map" and internal conditions.
    /// </summary>
    public class WorldState {

        // Visual
        public bool FugitiveInVision;
        public Position3D? LKP; // Last Known Position

        // Sound
        public List<NoiseEvent> DetectedNoises = new List<NoiseEvent>();

        // Other Agents
        public Dictionary<int, AgentPerception> OtherAgentsMap = new Dictionary<int, AgentPerception>();

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal State
        public float Alertness = 0f; // 0.0 to 1.0 limits
        public float Fatigue = 0f;   // 0.0 to 1.0 limits
        
        // Execution Context (Updated every frame by Unity wrapper)
        public float TimeDeltaContext = 0f;

        // Navigation
        public string CurrentLocationId = string.Empty;
        public HashSet<string> PastLocations = new HashSet<string>();

        /// <summary>
        /// Creates a deep copy of the state for HTN planning.
        /// The Planner tests 'ApplyEffects' on clones without mutating the real world state.
        /// </summary>
        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                LKP = this.LKP,
                PrisonerInCell = this.PrisonerInCell,
                Alertness = this.Alertness,
                Fatigue = this.Fatigue,
                CurrentLocationId = this.CurrentLocationId,
                TimeDeltaContext = this.TimeDeltaContext,
            };

            clone.DetectedNoises.AddRange(this.DetectedNoises);
            
            foreach (var kvp in this.OtherAgentsMap) {
                clone.OtherAgentsMap[kvp.Key] = kvp.Value;
            }
            
            foreach (var loc in this.PastLocations) {
                clone.PastLocations.Add(loc);
            }

            return clone;
        }
    }
}
