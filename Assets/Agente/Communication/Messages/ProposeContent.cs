namespace AgenticPrison.Communication.Messages {

    // Payload de un Propose: el coste que el participante estima para ejecutar la tarea.
    public class ProposeContent : IMessageContent {
        public float Cost;
    }
}
