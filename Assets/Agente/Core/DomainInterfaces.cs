using System.Collections.Generic;

namespace AgenticPrison.Core {

    /// <summary>
    /// Base interface for any task within the HTN (Compound or Primitive).
    /// </summary>
    public interface ITask {
    }

    public enum TaskExecutionStatus {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// Primitive tasks act as domain operators that mutate state and communicate directly with Unity wrappers.
    /// </summary>
    public interface IPrimitiveTask : ITask {
        bool CheckPreconditions(WorldState state);
        void ApplyEffects(WorldState state);

        /// <summary>
        /// Executes the physical action logic using the provided actuator wrapper interfaces.
        /// Executed continuously while active until Success or Failure.
        /// </summary>
        TaskExecutionStatus Execute(IMovable actuators, WorldState state);
    }

    /// <summary>
    /// Methods decompose high level decisions into lower level tasks.
    /// </summary>
    public interface IMethod {
        bool CheckPreconditions(WorldState state);
        
        /// <summary>
        /// Returns an ordered list of smaller tasks to achieve this method's intent.
        /// </summary>
        Queue<ITask> Decompose(WorldState state);
    }

    /// <summary>
    /// Compound tasks are high-level goals that try to decompose themselves via their assigned methods.
    /// </summary>
    public interface ICompoundTask : ITask {
        List<IMethod> Methods { get; }
    }

    /// <summary>
    /// An aggregation representing all possible output channels an agent has to interact with Unity.
    /// Primitive Tasks will cast or access the specific sub-interfaces they need (like IMovable).
    /// </summary>
    public interface IOperator {
        // Defined in the interfaces layer. This acts as a marker/container.
    }
}
