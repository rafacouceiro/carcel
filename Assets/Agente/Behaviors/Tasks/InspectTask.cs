using AgenticPrison.Core;
using AgenticPrison.Interfaces;

namespace AgenticPrison.Behaviors.Tasks {
    public class InspectTask : IPrimitiveTask {
        private float _inspectionTime;
        private float _timeSpent = 0f;

        public InspectTask(float duration = 2.0f) {
            _inspectionTime = duration;
        }

        public bool CheckPreconditions(WorldState state) {
            return true;
        }

        public void ApplyEffects(WorldState state) {
            state.Alertness -= 0.1f;
            if (state.Alertness < 0) state.Alertness = 0f;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            var driver = actuators as IAgentActuators;
            if (driver == null) return TaskExecutionStatus.Failure;

            if (_timeSpent == 0f) {
                driver.Movable.StopMoving();
                driver.Animator.TriggerInspect();
            }

            _timeSpent += state.TimeDeltaContext;

            if (_timeSpent >= _inspectionTime) {
                state.Alertness -= 0.1f;
                if (state.Alertness < 0) state.Alertness = 0f;
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }
    }
}
