using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Core {

    // Estado mínimo compartido por todos los agentes FIPA del sistema.
    //
    // Solo contiene los campos que la capa de comunicación (FIPAAgent, protocolos)
    // necesita leer o escribir. Los campos exclusivos de cada tipo de agente
    // (energía, navegación, visión, etc.) viven en sus implementaciones concretas.
    public interface IAgentState {

        // Identificador del agente — coincide con AgentId del bus de mensajes
        string AgentName { get; set; }

        // El prisionero sigue en la celda (asunción inicial)
        bool PrisonerInCell { get; set; }

        // Último sector donde se avistó al fugitivo; "[UNK]" = desconocido
        string FugitiveSectorId { get; set; }

        // Sector para el que ya se lanzó un CNP — evita relanzar para el mismo sector
        string PerimeteredSectorId { get; set; }

        // Última posición conocida del fugitivo (recibida vía visión propia o Inform)
        Vector3 LastKnownPosition { get; set; }

        // Instante del último avistamiento conocido
        float LastKnownPositionTime { get; set; }

        // true si este agente vio directamente al fugitivo (no por radio)
        bool seenByMe { get; set; }

        // Cola de subastas CNP pendientes de lanzar secuencialmente
        Queue<ContractTask> PendingCfps { get; }
    }
}
