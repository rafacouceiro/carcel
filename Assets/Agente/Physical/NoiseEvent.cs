using UnityEngine;

namespace AgenticPrison.Physical {
    public struct NoiseEvent {
        public Vector3 Position; // Origen real
        public float Volume;     // Radio de alcance en metros
        public string SourceTag; // Quién lo hizo (opcional)
        
        public NoiseEvent(Vector3 pos, float vol, string tag = "") {
            Position = pos;
            Volume = vol;
            SourceTag = tag;
        }
    }
}