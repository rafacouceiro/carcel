using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    public class LookAroundTask : IPrimitiveTask {
        private float _waitTime = 2f;
        private float _timer;

        public bool CheckPreconditions(WorldState state) => true;

        /// <summary>
        /// Maneja la parte "física" en tiempo real dentro de Unity.
        /// </summary>
        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            _timer += Time.deltaTime;

            // Comportamiento visual: El guardia gira la cabeza/cuerpo suavemente
            float angle = Mathf.Sin(Time.time * 2f) * 45f;
            actuators.RotateTo(angle); 

            if (_timer >= _waitTime) {
                // Al terminar, el sistema llamará a ApplyEffects automáticamente
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        /// <summary>
        /// Actualiza el WorldState (Estado del Mundo) tras completar la tarea.
        /// Esto es lo que el HTN usa para "simular" el plan.
        /// </summary>
        public void ApplyEffects(WorldState state) {
            state.Fatigue = Mathf.Max(0, state.Fatigue - 0.2f);
            // Debug.Log($"<color=cyan>[HTN Effect]</color> Fatiga recuperada. Actual: {state.Fatigue}");
        }
    }
}