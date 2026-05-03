namespace AgenticPrison.Communication {

    // Tipos de tarea física que el CommPlanner puede delegar al plano físico via IActionBridge
    public enum TaskType {
        GoToPosition,
        CoverPosition,
        InvestigateZone,
        ChaseTarget,
        MonitorZone,
        PatrolArea,
        TakeBreak
    }
}
