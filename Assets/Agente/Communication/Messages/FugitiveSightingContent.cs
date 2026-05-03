using UnityEngine;

namespace AgenticPrison.Communication.Messages {

    // Contenido para mensajes de broadcast INFORM que notifican un avistamiento del fugitivo.
    // Separamos esto del CFP para que el flujo de información sea continuo e independiente
    // de la negociación de tareas.
    public class FugitiveSightingContent : IMessageContent {
        public Vector3 Position;
        public float   Timestamp;
        public string  SectorId;
        public string  ReporterId;

        public FugitiveSightingContent(Vector3 pos, float time, string sector, string reporter) {
            Position   = pos;
            Timestamp  = time;
            SectorId   = sector;
            ReporterId = reporter;
        }
    }
}
