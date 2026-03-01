using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Lógica general de persecución cuando el preso es avistado
    public class ChaseMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // El prerrequisito fundamental es mantener contacto visual
            return state.FugitiveInVision;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            // Cambia luces a modo alerta roja y persigue a máxima velocidad
            subTasks.Enqueue(new ChangeFlashLight(Color.red));
            subTasks.Enqueue(new ChaseTask(6.5f));
            return subTasks;
        }
    }
}