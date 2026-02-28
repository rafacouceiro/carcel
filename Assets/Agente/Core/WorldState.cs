using System.Collections.Generic;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    public class WorldState {

        // Visual
        public bool FugitiveInVision = false;
        public Vector3 LastKnownPosition = Vector3.zero;
        public Vector3 CurrentPosition = Vector3.zero;
        
        // Audio
        public Vector3 LastNoisePosition = Vector3.zero;

        // Memory
        public float LastKnownPositionTime = 0f;
        public float LastNoisePositionTime = 0f;

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal state
        public float Energy = 100f;

        // Navigation & Memory
        public PrisonMap Map;
        public string AssignedQuadrantId = string.Empty;

        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                PrisonerInCell = this.PrisonerInCell,
                Energy = this.Energy,
                CurrentPosition = this.CurrentPosition,
                LastKnownPosition = this.LastKnownPosition,
                LastNoisePosition = this.LastNoisePosition,
                Map = this.Map,
                AssignedQuadrantId = this.AssignedQuadrantId,
                LastKnownPositionTime = this.LastKnownPositionTime,
                LastNoisePositionTime = this.LastNoisePositionTime
            };

            return clone;
        }
    }
}