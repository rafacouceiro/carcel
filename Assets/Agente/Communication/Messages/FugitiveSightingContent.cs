using UnityEngine;

namespace AgenticPrison.Communication.Messages {

    // Payload del Inform de avistamiento: posición, timestamp, sector y quién lo reportó.
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
