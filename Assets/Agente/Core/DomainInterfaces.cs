using System.Collections.Generic;

namespace AgenticPrison.Core {

    // Define una tarea genérica, base para el planificador HTN
    public interface ITask {
    }

    // Estados posibles durante la ejecución de tareas
    public enum TaskExecutionStatus {
        Running,
        Success,
        Failure
    }

    // Tareas primitivas: acciones concretas que modifican el mundo físico
    public interface IPrimitiveTask : ITask {
        // Verifica si la tarea se puede ejecutar bajo el estado actual
        bool CheckPreconditions(GuardWorldState state);
        
        // Aplica cambios teóricos al estado de simulación
        void ApplyEffects(GuardWorldState state);

        // Lógica física con los actuadores; se corre cada frame hasta el término
        TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state);
    }

    // Métodos: formas de descomponer tareas complejas en subtareas
    public interface IMethod {
        // Comprueba si este método es elegible para el estado actual
        bool CheckPreconditions(GuardWorldState state);
        
        // Devuelve una lista ordenada de tareas para lograr el método
        Queue<ITask> Decompose(GuardWorldState state);
    }

    // Tareas compuestas: objetivos de alto nivel que usan métodos para resolverse
    public interface ICompoundTask : ITask {
        // Conjunto de métodos disponibles para intentar descomponer la tarea
        List<IMethod> Methods { get; }
    }
}
