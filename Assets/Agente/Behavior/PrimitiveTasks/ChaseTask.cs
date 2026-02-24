using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class ChaseTask : IPrimitiveTask {
        private float _speed;

        public ChaseTask(float speed) {
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision; // Solo si lo vemos
        }

        public void ApplyEffects(WorldState state) {
            state.Fatigue += 0.05f; // Cansa mucho correr
        }

        // Este Execute se corre en cada frame mientras el status sea "Running"
        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // 1. Si lo perdemos de vista, la tarea fracasa inmediatamente
            if (!state.FugitiveInVision) {
                Debug.Log("🏃 [ChaseTask] ¡He perdido de vista al fugitivo!");
                return TaskExecutionStatus.Failure;
            }

            // 2. Actualizamos el destino constantemente con la LKP más reciente
            actuators.SetSpeed(_speed);
            actuators.SetDestination(state.LastKnownPosition);
            Debug.Log("🏃 [ChaseTask] ¡Estoy persiguiendo!");

            // 3. Comprobamos si estamos lo suficientemente cerca para atraparlo
            float distance = Vector3.Distance(state.CurrentPosition, state.LastKnownPosition);
            if (distance < 1.5f) {
                Debug.Log("🎯 [ChaseTask] ¡Estoy a rango de captura!");
                return TaskExecutionStatus.Success; // Terminamos para que el Planner active CatchMethod
            }

            return TaskExecutionStatus.Running;
        }
    }
}