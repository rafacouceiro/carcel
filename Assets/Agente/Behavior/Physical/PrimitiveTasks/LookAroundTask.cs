using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    // Tarea primitiva: El guardia gira su cabeza brevemente inspeccionando sus alrededores
    public class LookAroundTask : IPrimitiveTask {
        private float _waitTime = 2f;
        private float _timer;
        private float _centerRotation; // Orientación neutral al llegar
        private bool _isInitialized = false;

        public bool CheckPreconditions(WorldState state) {
            return true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            if (!_isInitialized) {
                // Fija la dirección base inicial
                _centerRotation = actuators.GetRotation(); 
                _isInitialized = true;
            }

            _timer += Time.deltaTime;

            // Movimiento oscilatorio natural para el escaneo (45 grados)
            float angleOffset = Mathf.Sin(Time.time * 2f) * 45f;
            actuators.RotateTo(_centerRotation + angleOffset); 

            // Concluir revisión transcurrido el lapso de tiempo
            if (_timer >= _waitTime) {
                
                // Repostar nivel de energía como recompensa menor
                state.Energy = Mathf.Min(100f, state.Energy + 20f);
                
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        // Simulación de alivio de cansancio en el gestor HTN
        public void ApplyEffects(WorldState state) {
            state.Energy = Mathf.Min(100f, state.Energy + 20f);
        }
    }
}