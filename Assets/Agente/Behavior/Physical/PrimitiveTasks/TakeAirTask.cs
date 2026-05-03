using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    // Tarea primitiva: Inmoviliza al agente temporalmente para recuperar el aliento
    public class TakeBreathTask : IPrimitiveTask {
        
        private float _waitTime = 2f; // Segundos obligatorios de reposo
        private float _timer = 0f;

        public bool CheckPreconditions(GuardWorldState state) {
            // Siempre se puede decidir pausar para descansar
            return true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            
            // Pausa estática progresando el contador
            _timer += Time.deltaTime;

            if (_timer >= _waitTime) {
                // Ganancia energética real
                state.Energy = Mathf.Min(100f, state.Energy + 30f);
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        public void ApplyEffects(GuardWorldState state) {
            // Ganancia energética para evaluación en la imaginación
            state.Energy = Mathf.Min(100f, state.Energy + 30f);
        }
    }
}