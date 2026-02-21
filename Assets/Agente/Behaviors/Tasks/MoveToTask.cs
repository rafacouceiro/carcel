using AgenticPrison.Core;
using AgenticPrison.Core.Math;
using AgenticPrison.Interfaces;

namespace AgenticPrison.Behaviors.Tasks {
    public class MoveToTask : IPrimitiveTask {
        private Position3D _destination;
        private bool _isStarted = false;

        public MoveToTask(Position3D destination) {
            _destination = destination;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.Fatigue < 0.95f; 
        }

        public void ApplyEffects(WorldState state) {
            state.Fatigue += 0.05f; 
            state.CurrentLocationId = "MovingTo: " + _destination;
            state.PastLocations.Add(state.CurrentLocationId);
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            var driver = actuators as IAgentActuators;
            if (driver == null) return TaskExecutionStatus.Failure;

            if (!_isStarted) {
                driver.Movable.SetDestination(_destination);
                _isStarted = true;
                return TaskExecutionStatus.Running;
            }

            if (!driver.Movable.IsMoving()) {
                state.Fatigue += 0.05f; 
                state.PastLocations.Add(_destination.ToString());
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }
    }
}
