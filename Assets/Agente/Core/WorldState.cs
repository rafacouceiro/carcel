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
        public Vector3 LastNoisePosition;

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal state
        public float Fatigue = 0f;

        // Navigation & Memory
        public PrisonMap Map;
        public string AssignedQuadrantId = string.Empty;

        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                PrisonerInCell = this.PrisonerInCell,
                Fatigue = this.Fatigue,
                CurrentPosition = this.CurrentPosition,
                LastKnownPosition = this.LastKnownPosition,
                LastNoisePosition = this.LastNoisePosition,
                Map = this.Map,
                AssignedQuadrantId = this.AssignedQuadrantId
            };

            return clone;
        }
    }
}