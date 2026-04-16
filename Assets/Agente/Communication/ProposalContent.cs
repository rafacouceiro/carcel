namespace AgenticPrison.Communication {

    // Payload del mensaje Propose en el protocolo Contract Net
    public class ProposalContent {
        public float  EstimatedCost;  // longitud del camino NavMesh hasta el objetivo
        public string ExecutorId;     // AgentId del agente que propone
    }
}
