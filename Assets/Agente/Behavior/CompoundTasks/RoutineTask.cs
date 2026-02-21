using AgenticPrison.Core;
using System.Collections.Generic;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {
    
    public class RoutineTask : ICompoundTask {
        
        public List<IMethod> Methods { get; } = new List<IMethod>();

        public RoutineTask() {
            // Le damos a esta tarea la capacidad de resolverse usando la patrulla
            Methods.Add(new PatrolMethod());
        }
    }
}