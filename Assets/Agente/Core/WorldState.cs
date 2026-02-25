using System.Collections.Generic;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    public class WorldState {

        // Visual
        public bool FugitiveInVision;
        public Vector3 LastKnownPosition;
        public Vector3 CurrentPosition;
        
        // Audio
        public Vector3 LastKnownNoisePosition;

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal State
        public bool Alertness = false;
        public float Fatigue = 0f;

        // Navigation & Memory
        public PrisonMap Map;
        public string AssignedQuadrantId = string.Empty;

        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                PrisonerInCell = this.PrisonerInCell,
                Alertness = this.Alertness,
                Fatigue = this.Fatigue,
                CurrentPosition = this.CurrentPosition,
                LastKnownPosition = this.LastKnownPosition,
                LastKnownNoisePosition = this.LastKnownNoisePosition,
                Map = this.Map, // Todos comparten LA MISMA instancia del mapa
                AssignedQuadrantId = this.AssignedQuadrantId
            };

            return clone;
        }
    }
}