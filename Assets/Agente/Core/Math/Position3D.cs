using System;

namespace AgenticPrison.Core.Math {
    /// <summary>
    /// A pure C# representation of a 3D coordinate point.
    /// Used to decouple the HTN logic from Unity's Vector3 inside UnityEngine.CoreModule.
    /// </summary>
    public struct Position3D {
        public float X;
        public float Y;
        public float Z;

        public Position3D(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public static float Distance(Position3D a, Position3D b) {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
