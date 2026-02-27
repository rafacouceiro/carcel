using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    
    public class TakeBreathTask : IPrimitiveTask {
        
        private float _waitTime = 2f; // Un par de segundos para tomar aire
        private float _timer = 0f;

        public bool CheckPreconditions(WorldState state) {
            // Siempre se puede intentar tomar un respiro
            return true;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // Simplemente dejamos pasar el tiempo, el agente se queda totalmente quieto
            _timer += Time.deltaTime;

            // Si ya ha pasado el tiempo de descanso
            if (_timer >= _waitTime) {
                
                // --- RECUPERACIÓN REAL ---
                // Sube la energía en 20, asegurándonos de no pasarnos de 100
                state.Energy = Mathf.Min(100f, state.Energy + 30f);
                
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        public void ApplyEffects(WorldState state) {
            
            // --- RECUPERACIÓN EN LA IMAGINACIÓN DEL PLANIFICADOR ---
            state.Energy = Mathf.Min(100f, state.Energy + 30f);
        }
    }
}