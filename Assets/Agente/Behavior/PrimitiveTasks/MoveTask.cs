using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class MoveTask : IPrimitiveTask {
        
        private Vector3 _target;
        private float _speed;
        private bool _isActionStarted = false;

        // Variables para el ruido
        private float _noiseTimer = 0f;
        private const float StepInterval = 0.5f; // Tiempo entre pasos

        public MoveTask(Vector3 target, float speed) {
            _target = target;
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            return _target != Vector3.zero && state.Energy >= CalculateEnergyCost(_speed);
        }

        public void ApplyEffects(WorldState state) {
            state.CurrentPosition = _target;
            state.Energy = Mathf.Max(0, state.Energy - CalculateEnergyCost(_speed));
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            if (!_isActionStarted) {
                actuators.SetSpeed(_speed);
                actuators.SetDestination(_target);
                _isActionStarted = true;
            }

            // --- GENERACIÓN DE RUIDO ---
            _noiseTimer -= Time.deltaTime;
            if (_noiseTimer <= 0f) {
                float noiseVolume = CalculateNoiseVolume(_speed);
                // Emitimos el ruido en la posición actual
                NoiseManager.EmitNoise(new NoiseEvent(state.CurrentPosition, noiseVolume, state.AgentName));
                _noiseTimer = StepInterval; // Reseteamos el temporizador
            }

            if (!actuators.IsMoving()) {
                state.Energy = Mathf.Max(0, state.Energy - CalculateEnergyCost(_speed));
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        private float CalculateEnergyCost(float currentSpeed) {
            float t = Mathf.InverseLerp(3.0f, 6.5f, currentSpeed);
            return Mathf.Lerp(1f, 5f, t);
        }

        // --- CÁLCULO PROPORCIONAL DE RUIDO ---
        private float CalculateNoiseVolume(float currentSpeed) {
            float t = Mathf.InverseLerp(3.0f, 6.5f, currentSpeed);
            return Mathf.Lerp(7f, 20f, t); // Entre 7 y 20 dependiendo de la velocidad
        }
    }
}