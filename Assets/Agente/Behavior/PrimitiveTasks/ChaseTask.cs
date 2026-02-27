using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class ChaseTask : IPrimitiveTask {
        private float _speed;

        public ChaseTask(float speed) {
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            // Empieza a perseguir si tiene un mínimo de energía
            return state.FugitiveInVision && state.Energy >= 5f; 
        }

        public void ApplyEffects(WorldState state) {
            // En la imaginación, asumimos que gastamos algo de energía inicial
            state.Energy = Mathf.Max(0, state.Energy - 5f); 
            state.CurrentPosition = state.LastKnownPosition; 
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // 1. Si se pierde de vista al fugitivo, fracasa la persecución
            if (!state.FugitiveInVision) {
                return TaskExecutionStatus.Failure;
            }

            // 2. Si se queda sin energía de tanto correr, fracasa (jadeará y descansará)
            if (state.Energy <= 0f) {
                Debug.LogWarning("<color=orange>El guardia se ha quedado sin aliento persiguiendo.</color>");
                return TaskExecutionStatus.Failure;
            }

            actuators.SetSpeed(_speed);
            actuators.SetDestination(state.LastKnownPosition);
            
            // Le restamos 3 puntos POR SEGUNDO, no por fotograma.
            state.Energy = Mathf.Max(0, state.Energy - (3f * Time.deltaTime)); 

            // Comprobar si estamos lo suficientemente cerca para atraparlo
            float distance = Vector3.Distance(state.CurrentPosition, state.LastKnownPosition);
            if (distance < 1.5f) { 
                return TaskExecutionStatus.Success; 
            }

            return TaskExecutionStatus.Running;
        }
    }
}