using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    public class LookAroundTask : IPrimitiveTask {
        private float _waitTime = 2f;
        private float _timer;
        private float _centerRotation; // Rotación base donde se detuvo
        private bool _isInitialized = false;

        public bool CheckPreconditions(WorldState state) {
            return true;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            if (!_isInitialized) {
                // Guardamos la rotación que tenía al llegar
                _centerRotation = actuators.GetRotation(); 
                _isInitialized = true;
            }

            _timer += Time.deltaTime;

            // Oscilación de 45 grados respecto al centro
            float angleOffset = Mathf.Sin(Time.time * 2f) * 45f;
            actuators.RotateTo(_centerRotation + angleOffset); 

            return (_timer >= _waitTime) ? TaskExecutionStatus.Success : TaskExecutionStatus.Running;
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