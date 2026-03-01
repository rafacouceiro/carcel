using AgenticPrison.Core;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: Persigue al fugitivo físicamente hasta atraparlo
    public class ChaseTask : IPrimitiveTask {
        private float _speed;
        private float _noiseTimer = 0f;
        private const float RunStepInterval = 0.3f; // Frecuencia de ruido al correr

        public ChaseTask(float speed) {
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            // Se puede iniciar la persecución si hay visión directa y energía suficiente
            return state.FugitiveInVision && state.Energy >= 5f; 
        }

        public void ApplyEffects(WorldState state) {
            // Simulación física y energética de la persecución para el planificador HTN
            state.Energy = Mathf.Max(0, state.Energy - 5f); 
            state.CurrentPosition = state.LastKnownPosition; 
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            
            // 1. Fracaso por pérdida visual del blanco
            if (!state.FugitiveInVision) {
                return TaskExecutionStatus.Failure;
            }

            // 2. Fracaso por agotamiento físico extremo
            if (state.Energy <= 0f) {
                Debug.LogWarning("<color=orange>El guardia se ha quedado sin aliento persiguiendo.</color>");
                return TaskExecutionStatus.Failure;
            }

            actuators.SetSpeed(_speed);
            actuators.SetDestination(state.LastKnownPosition);
            
            // Drenaje continuo de energía en tiempo real (por segundo)
            state.Energy = Mathf.Max(0, state.Energy - (3f * Time.deltaTime)); 
            
            // Emisión de alertas sonoras al correr
            _noiseTimer -= Time.deltaTime;
            if (_noiseTimer <= 0f) {
                NoiseManager.EmitNoise(new NoiseEvent(state.CurrentPosition, 20f, state.AgentName));
                _noiseTimer = RunStepInterval;
            }

            // Condición de captura si se reduce suficientemente la distancia
            float distance = Vector3.Distance(state.CurrentPosition, state.LastKnownPosition);
            if (distance < 1.5f) { 
                return TaskExecutionStatus.Success; 
            }

            return TaskExecutionStatus.Running;
        }
    }
}