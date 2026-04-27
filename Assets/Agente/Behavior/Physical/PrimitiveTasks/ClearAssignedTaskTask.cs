using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: señaliza que la tarea asignada por contrato ha concluido.
    // Al poner AssignedTask a null, permite que ContractNetParticipant detecte
    // la finalización en su tick de tiempo y envíe el InformDone al iniciador.
    // También decrementa SweepProtocolsActive para que el líder pueda detectar
    // que todos los sweepers (él incluido) han terminado.
    public class ClearAssignedTaskTask : IPrimitiveTask {

        public bool CheckPreconditions(WorldState state) => state.AssignedTask != null;

        public void ApplyEffects(WorldState state) {
            state.AssignedTask = null;
            if (state.SweepProtocolsActive > 0) state.SweepProtocolsActive--;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            state.AssignedTask = null;
            if (state.SweepProtocolsActive > 0) state.SweepProtocolsActive--;
            return TaskExecutionStatus.Success;
        }
    }
}
