namespace AgenticPrison.Communication.Messages {

    // Payload de un mensaje Propose en el protocolo Contract Net.
    // Wrappea el coste estimado para garantizar type-safety en ACLMessage.Content.
    public class ProposeContent : IMessageContent {
        public float Cost;
    }
}
