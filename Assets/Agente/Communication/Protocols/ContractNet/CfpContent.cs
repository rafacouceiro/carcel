using UnityEngine;

using AgenticPrison.Communication;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Contenido de un mensaje CFP (Call For Proposals) del protocolo Contract Net.
    // Incluye el contexto de la situación que motiva la subasta, no solo la tarea en sí.
    public class CfpContent {
        public Vector3      FugitivePosition;      // última posición conocida del fugitivo
        public float        FugitivePositionTime;  // instante en que se observó esa posición
        public ContractTask Task;                  // tarea concreta que se subasta
        public string       SectorId;              // sector de operación — detecta cambios de sector
    }
}
