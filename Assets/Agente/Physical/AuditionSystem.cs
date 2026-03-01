using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Physical {

    // Estructura de datos referida a los ruidos producidos
    public struct NoiseEvent {
        public Vector3 Position; // Origen físico del sonido en el mapa
        public float Volume;     // Volumen que determina el radio de alcance
        public string emisor;   // Identificador del emisor para filtrar ruidos propios
        
        public NoiseEvent(Vector3 pos, float vol, string emisor) {
            Position = pos;
            Volume = vol;
            this.emisor = emisor;
        }
    }
    
    // Gestor estático para emitir sonidos y notificar a los oyentes cercanos
    public static class NoiseManager {
        // Registro de los agentes con capacidad auditiva en escena
        private static List<INoiseReceiver> _receivers = new List<INoiseReceiver>();

        public static void RegisterReceiver(INoiseReceiver receiver) => _receivers.Add(receiver);
        public static void UnregisterReceiver(INoiseReceiver receiver) => _receivers.Remove(receiver);

        // Envía el estímulo acústico a los agentes basándose en la distancia y alcance
        public static void EmitNoise(NoiseEvent noise) {
            foreach (var receiver in _receivers) {
                float dist = Vector3.Distance(noise.Position, receiver.GetPosition());
                if (dist <= noise.Volume) {
                    receiver.OnNoiseHeard(noise);
                }
            }
        }
    }

    // Interfaz que deben implementar los objetos capaces de oír
    public interface INoiseReceiver {
        Vector3 GetPosition(); // Posición en la que se ubica el oído
        void OnNoiseHeard(NoiseEvent noise); // Callback al percibir el sonido
    }
}
