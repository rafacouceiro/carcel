using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Comportamiento por defecto ante la falta de estímulos inusuales
    public class RoutineTask : ICompoundTask {
        
        // Combina el avance de la ronda de vigilancia con fases de descanso
        public List<IMethod> Methods { get; }

        public RoutineTask() {
            Methods = new List<IMethod> {
                new PatrolMethod(),
                new SelectEnergyRecovery()
            };
        }
    }
}