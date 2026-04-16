namespace AgenticPrison.Communication {

    // Prioridad de una tarea física — determina si un agente acepta bids entrantes
    public enum TaskPriority {
        Idle        = 0,
        Patrol      = 1,
        EnergyRest  = 2,
        InvestNoise = 3,
        CoverExit   = 4,
        Investigate = 4,
        Chase       = 5,   // nunca se interrumpe por comunicación
        GameOver    = 6,   // nunca se interrumpe por comunicación
    }
}
