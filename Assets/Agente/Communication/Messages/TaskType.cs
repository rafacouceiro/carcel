namespace AgenticPrison.Communication.Messages {

    // Tipos de tarea que puede resultar de un contrato FIPA
    public enum TaskType {
        BlockSector,   // patrulla cíclica entre waypoints de bloqueo del sector
        SweepSector,   // rastreo sistemático de habitaciones asignadas del sector
    }
}
