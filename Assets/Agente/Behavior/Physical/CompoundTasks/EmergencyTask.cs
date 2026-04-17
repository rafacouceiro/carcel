using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Situación crítica donde el fugitivo ha sido detectado visualmente
    public class EmergencyTask : ICompoundTask {
        
        // Atrapa si está muy cerca, de lo contrario lo persigue
        public List<IMethod> Methods { get; }

        public EmergencyTask() {
            Methods = new List<IMethod> {
                new CatchMethod(),
                new ChaseMethod()
            };
        }
    }
}