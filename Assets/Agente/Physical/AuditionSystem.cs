using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Physical {

    public struct NoiseEvent {
        public Vector3 Position; // Origen real
        public float Volume;     // Radio de alcance en metros
        public string emisor;   // Nombre del emisor
        
        public NoiseEvent(Vector3 pos, float vol, string emisor) {
            Position = pos;
            Volume = vol;
            this.emisor = emisor;
        }
    }
    
    public static class NoiseManager {
        // Lista de todos los agentes que pueden oír
        private static List<INoiseReceiver> _receivers = new List<INoiseReceiver>();

        public static void RegisterReceiver(INoiseReceiver receiver) => _receivers.Add(receiver);
        public static void UnregisterReceiver(INoiseReceiver receiver) => _receivers.Remove(receiver);

        public static void EmitNoise(NoiseEvent noise) {
            foreach (var receiver in _receivers) {
                float dist = Vector3.Distance(noise.Position, receiver.GetPosition());
                if (dist <= noise.Volume) {
                    receiver.OnNoiseHeard(noise);
                }
            }
        }
    }

    public interface INoiseReceiver {
        Vector3 GetPosition();
        void OnNoiseHeard(NoiseEvent noise);
    }
}
