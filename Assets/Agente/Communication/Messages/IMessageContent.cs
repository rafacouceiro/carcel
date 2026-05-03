namespace AgenticPrison.Communication.Messages {

    // Tipo base para todos los payloads de mensajes ACL.
    // Garantiza type-safety en ACLMessage.Content sin necesidad de serialización.
    public interface IMessageContent { }
}
