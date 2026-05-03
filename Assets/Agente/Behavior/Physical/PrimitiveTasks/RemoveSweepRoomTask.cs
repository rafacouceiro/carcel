using AgenticPrison.Core;
using AgenticPrison.Physical;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: elimina una habitación de la lista de rastreo pendiente.
    // Se encola tras visitar cada sala durante un sweep asignado, de modo que
    // si el plan se interrumpe (ruido, emergencia), la reanudación no repite salas ya vistas.
    public class RemoveSweepRoomTask : IPrimitiveTask {

        readonly RoomNode _room;

        public RemoveSweepRoomTask(RoomNode room) {
            _room = room;
        }

        public bool CheckPreconditions(GuardWorldState state) {
            return state.AssignedTask != null
                && state.AssignedTask.SweepRooms != null
                && state.AssignedTask.SweepRooms.Contains(_room);
        }

        public void ApplyEffects(GuardWorldState state) {
            state.AssignedTask?.SweepRooms?.Remove(_room);
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            if (state.AssignedTask?.SweepRooms == null) return TaskExecutionStatus.Failure;
            state.AssignedTask.SweepRooms.Remove(_room);
            return TaskExecutionStatus.Success;
        }
    }
}
