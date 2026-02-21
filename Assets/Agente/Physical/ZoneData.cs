using System.Collections.Generic;
using AgenticPrison.Core.Math;

namespace AgenticPrison.Core {
    /// <summary>
    /// Representación abstracta y pura de una zona para el HTN.
    /// </summary>
    public class ZoneData {
        public string Id;
        public bool IsExit;
        public Position3D Center;
        public List<Position3D> PatrolPoints = new List<Position3D>();
    }
}