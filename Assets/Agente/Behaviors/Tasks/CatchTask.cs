using AgenticPrison.Core;
using AgenticPrison.Interfaces;

namespace AgenticPrison.Behaviors.Tasks {
    public class CatchTask : IPrimitiveTask {
        
        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision; 
        }

        public void ApplyEffects(WorldState state) {
            state.PrisonerInCell = true;
            state.Alertness = 1.0f;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            var driver = actuators as IAgentActuators;
            if (driver == null) return TaskExecutionStatus.Failure;

            driver.Animator.TriggerCatch();
            
            // Assume completion immediately for puzzle HTN
            state.PrisonerInCell = true;
            state.Alertness = 1.0f;
            return TaskExecutionStatus.Success;
        }
    }
}
