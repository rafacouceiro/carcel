using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;

namespace AgenticPrison.Agents.Tools {

    // Helper estático para obtener habitaciones adyacentes a una posición dada.
    // Usa el grafo connectedRooms del RoomNode actual.
    public static class AdjacentRoomGenerator {

        // Devuelve hasta 'count' habitaciones conectadas al nodo más cercano a 'position'
        public static List<RoomNode> GetAdjacentRooms(Vector3 position, PrisonMap map, int count = 2) {
            RoomNode current = map.GetCurrentNode(position);
            if (current == null || current.connectedRooms == null)
                return new List<RoomNode>();

            var result = new List<RoomNode>();
            foreach (RoomNode room in current.connectedRooms) {
                if (result.Count >= count) break;
                result.Add(room);
            }
            return result;
        }
    }
}
