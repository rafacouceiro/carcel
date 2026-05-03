using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Core {

    // Estado mínimo compartido por todos los agentes FIPA del sistema.
    //
    // Contiene únicamente los campos que la capa de comunicación (FIPAAgent,
    // protocolos) necesita leer o escribir. Los campos exclusivos de cada tipo
    // de agente viven en sus subclases concretas (WorldState, CameraState).
    public abstract class AgentState {

        // Identificador del agente — coincide con AgentId del bus de mensajes
        public string AgentName = string.Empty;

        // El prisionero sigue en la celda (asunción inicial)
        public bool PrisonerInCell = true;

        // Último sector donde se avistó al fugitivo; "[UNK]" = desconocido
        public string FugitiveSectorId = "[UNK]";

        // Sector para el que ya se lanzó un CNP — evita relanzar para el mismo sector
        public string PerimeteredSectorId = string.Empty;

        // Última posición conocida del fugitivo (visión propia o Inform recibido)
        public Vector3 LastKnownPosition = Vector3.zero;

        // Instante del último avistamiento conocido
        public float LastKnownPositionTime = 0f;

        // true si este agente vio directamente al fugitivo (no por radio)
        public bool seenByMe = false;

        // Cola de subastas CNP pendientes de lanzar secuencialmente
        public Queue<ContractTask> PendingCfps = new Queue<ContractTask>();
    }
}
