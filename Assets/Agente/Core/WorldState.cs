using System.Collections.Generic;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    public class WorldState {

        // Visual
        public bool FugitiveInVision;
        public Vector3 LastKnownPosition;
        public Vector3 CurrentPosition;

        // Logical conditions
        public bool PrisonerInCell = true;

        // Internal State
        public float Alertness = 0f;
        public float Fatigue = 0f;

        // Navigation & Memory
        public string CurrentLocationId = string.Empty;

        // --- NUEVO: Spatial Knowledge ---
        // Lista de las habitaciones que tiene que patrullar
        public List<RoomNode> AssignedQuadrant = new List<RoomNode>();
        
        // Donde estoy ahora (vital para que el DFS sepa por dónde empezar)
        public RoomNode CurrentRoomNode; 

        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                PrisonerInCell = this.PrisonerInCell,
                Alertness = this.Alertness,
                Fatigue = this.Fatigue,
                CurrentLocationId = this.CurrentLocationId,
                CurrentPosition = this.CurrentPosition,
                CurrentRoomNode = this.CurrentRoomNode,
                LastKnownPosition = this.LastKnownPosition,
                
                // Hacemos una copia superficial (shallow copy) de la lista. 
                // Clonamos la lista, pero NO clonamos los objetos RoomNode de Unity.
                AssignedQuadrant = new List<RoomNode>(this.AssignedQuadrant)
            };

            return clone;
        }
    }
}