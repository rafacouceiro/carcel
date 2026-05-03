namespace AgenticPrison.Communication.Messages {

    // Respuesta a un QueryIf: distancia del guardia al punto de ruido.
    public class QueryIfResponseContent : IMessageContent {
        public float Distance;
    }
}
