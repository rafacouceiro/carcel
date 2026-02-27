using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class ChaseTask : IPrimitiveTask {
        private float _speed;

        public ChaseTask(float speed) {
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.Energy >= 5f; // Ejecutar tarea si vemos al fugitivo y tenemos energia
        }

        public void ApplyEffects(WorldState state) {
            state.Energy = Mathf.Max(0, state.Energy - 5f); // Efectos de cansancio
            state.CurrentPosition = state.LastKnownPosition; // Actualizamos la posicion del agente
        }

        // Este Execute se corre en cada frame mientras el status sea "Running"
        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // Si se pierde de vista al fugitivo la tarea fracasa
            if (!state.FugitiveInVision) {
                return TaskExecutionStatus.Failure;
            }

            // Actualizar el destino con la última posición conocida 
            // para evitar lag en la persecución
            actuators.SetSpeed(_speed);
            actuators.SetDestination(state.LastKnownPosition);
            state.Energy = Mathf.Max(0, state.Energy - 5f); // Efectos de cansancio

            // Comprobar si estamos lo suficientemente cerca para atraparlo
            float distance = Vector3.Distance(state.CurrentPosition, state.LastKnownPosition);
            if (distance < 1.5f) { 
                return TaskExecutionStatus.Success; // Si estamos en rango de captura marcar la tarea como exitosa
            }

            return TaskExecutionStatus.Running;
        }
    }
}