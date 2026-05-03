using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Situación crítica donde el fugitivo ha sido detectado visualmente
    public class EmergencyTask : ICompoundTask {
        
        // Atrapa si está muy cerca, lo persigue si está en rango, o predice su ruta si se acaba de perder
        public List<IMethod> Methods { get; }

        public EmergencyTask() {
            Methods = new List<IMethod> {
                new CatchMethod(),
                new ChaseMethod(),
                new PredictivePursuitMethod()
            };
        }
    }
}