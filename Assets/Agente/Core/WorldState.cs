using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Core {

    // Estado base de cualquier agente del sistema. Solo tiene los campos que
    // la capa de comunicación necesita — lo demás va en cada subclase.
    public abstract class WorldState {

        public string AgentName = string.Empty;

        // Mientras sea true, el agente asume que el preso no se ha fugado
        public bool PrisonerInCell = true;

        // Sector donde se vio al fugitivo por última vez. "[UNK]" si no se sabe.
        public string FugitiveSectorId = "[UNK]";

        // Sector para el que ya lanzamos un CNP, para no relanzarlo
        public string PerimeteredSectorId = string.Empty;

        public Vector3 LastKnownPosition     = Vector3.zero;
        public float   LastKnownPositionTime = 0f;

        // true solo si este agente lo vio con sus propios ojos, no por radio
        public bool seenByMe = false;

        // Las subastas CNP se lanzan de una en una; aquí se acumulan las pendientes
        public Queue<ContractTask> PendingCfps = new Queue<ContractTask>();
    }
}
