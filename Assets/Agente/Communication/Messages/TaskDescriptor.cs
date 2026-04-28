using UnityEngine;

namespace AgenticPrison.Communication.Messages {

    // Descriptor de tarea pasado entre agentes en el protocolo Contract Net
    public class TaskDescriptor {
        public TaskType    Type;
        public Vector3     TargetPosition;
        public string      TargetAgentId;
        public float       Urgency;
        public float       Deadline;
        public string      ContractId;
    }
}
