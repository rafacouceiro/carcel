using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    
    // Método HTN: Orquesta una pausa breve para tomar oxígeno
    public class TakeBreakMethod : IMethod {
        
        public bool CheckPreconditions(GuardWorldState state) {
            // Accesible en cualquier momento (suele evaluarse al final del árbol por baja prioridad)
            return true; 
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            // Color naranja representa momento de reposo activo
            subTasks.Enqueue(new ChangeFlashLight(Color.orange));
            subTasks.Enqueue(new TakeBreathTask());
            return subTasks;
        }
    }
}