using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.RootTask {

    // Tarea principal (RootTask) que define la prioridad de comportamiento del agente.
    public class BeGuard : ICompoundTask {

        public List<IMethod> Methods { get; }

        public BeGuard() {
            Methods = new List<IMethod> {
                new SelectEmergency(),          // prio 1: persecución / captura
                new BlockingPositionMethod(),   // prio 2: blocker — ciclo de waypoints de perímetro
                new InvestigateNoiseMethod(),   // prio 3 ┐
                new WideSweepMethod(),          // prio 4 ├─ investigación
                new InvestigateLocationMethod(),// prio 5 ┘
                new SelectRoutine()             // prio 6: rutina normal
            };
        }
    }
}