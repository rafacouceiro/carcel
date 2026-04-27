using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;

namespace AgenticPrison.Agents.Tools {

    // Helper estático para obtener habitaciones candidatas a cubrir dada una posición.
    // Explora el grafo de RoomNode hasta grado 2 (vecinos directos y sus vecinos)
    // y devuelve las 'count' con mayor grado (más conexiones), priorizando nodos bien
    // conectados que pueden cortar más rutas de escape.
    public static class AdjacentRoomGenerator {

        // Devuelve hasta 'count' habitaciones alcanzables en 1 o 2 saltos desde la
        // habitación más cercana a 'position', ordenadas por grado descendente.
        public static List<RoomNode> GetAdjacentRooms(Vector3 position, PrisonMap map, int count = 3) {
            RoomNode origin = map.GetCurrentNode(position);
            if (origin == null) return new List<RoomNode>();

            // Recopilar candidatos de grado 1 y 2, sin repetir ni incluir el origen
            var visited = new HashSet<RoomNode> { origin };
            var candidates = new List<RoomNode>();

            foreach (RoomNode first in origin.connectedRooms) {
                if (first == null || visited.Contains(first)) continue;
                visited.Add(first);
                candidates.Add(first);

                foreach (RoomNode second in first.connectedRooms) {
                    if (second == null || visited.Contains(second)) continue;
                    visited.Add(second);
                    candidates.Add(second);
                }
            }

            // Ordenar por grado descendente (más conexiones = nodo más estratégico)
            candidates.Sort((a, b) => b.connectedRooms.Count.CompareTo(a.connectedRooms.Count));

            return candidates.Count <= count ? candidates : candidates.GetRange(0, count);
        }
    }
}
