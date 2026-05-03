namespace AgenticPrison.Communication.Messages {

    // Contenido de un mensaje CFP (Call For Proposals) del protocolo Contract Net.
    // Minimalista: solo contiene la tarea subastada.
    public class CfpContent : IMessageContent {
        public ContractTask Task;      // tarea concreta que se subasta
    }
}
