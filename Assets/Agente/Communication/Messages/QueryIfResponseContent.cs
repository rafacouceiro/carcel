namespace AgenticPrison.Communication.Messages {

    // Payload de un Inform en respuesta a un QueryIf.
    // El participante informa de su distancia al punto sospechoso;
    // el iniciador decide cómo interpretarla vía callback.
    public class QueryIfResponseContent : IMessageContent {
        public float Distance;
    }
}
