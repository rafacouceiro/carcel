using AgenticPrison.Core;

namespace AgenticPrison.Agents.Camera {

    // Estado del mundo de la cámara de vigilancia.
    //
    // La cámara no tiene cuerpo físico: no necesita energía, navegación,
    // audición ni memoria de compañeros. Solo rastrea el sector del fugitivo
    // y gestiona la cola de subastas CNP que lanza al detectarlo.
    //
    // Todos los campos de coordinación FIPA (FugitiveSectorId, PendingCfps,
    // PrisonerInCell, etc.) se heredan de WorldState.
    public class CameraWorldState : WorldState { }
}
