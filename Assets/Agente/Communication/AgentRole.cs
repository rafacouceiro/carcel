namespace AgenticPrison.Communication {

    // Rol momentáneo asignado a un agente durante una operación de sector
    public enum AgentRole {
        None,     // sin rol activo — comportamiento normal
        Blocker,  // bloquear un punto de salida del sector cíclicamente
        Sweeper   // rastrar habitaciones interiores del sector
    }
}
