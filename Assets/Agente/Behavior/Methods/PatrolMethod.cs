using AgenticPrison.Core;
using System.Collections.Generic;
using AgenticPrison.Behavior.PrimitiveTasks; // Tu namespace de tareas

namespace AgenticPrison.Behavior.Methods {
    
    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // Por ahora, siempre podemos intentar patrullar si no hay emergencias
            return true; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // 1. Pensar a qué zona ir
            subTasks.Enqueue(new ChoosePatrolZoneTask());
            
            // 2. (Opcional) Si quisieras, podrías meter aquí un "MoveToTask" genérico para ir hasta la zona, 
            // pero el PatrolZoneTask ya manda al agente al primer punto de la zona usando SetDestination, 
            // así que el NavMeshDriver hará el viaje largo automáticamente.

            // 3. Patrullar los puntos de la zona
            subTasks.Enqueue(new PatrolZoneTask());
            
            return subTasks;
        }
    }
}