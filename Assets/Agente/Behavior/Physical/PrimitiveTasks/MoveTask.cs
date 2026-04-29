using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: Navegación genérica hacia un objetivo emitiendo ruido
    public class MoveTask : IPrimitiveTask {

        private Vector3 _target;
        private float _speed;
        private bool _isActionStarted = false;

        // Variables de emisión de sonido
        private float _noiseTimer = 0f;
        private const float StepInterval = 0.5f; // Cadencia de pisadas

        // Detección de atasco: si el agente no avanza, ceder el paso
        private Vector3 _lastCheckedPosition;
        private float _stuckTimer = 0f;
        private const float StuckCheckInterval = 1.0f; // segundos entre comprobaciones
        private const float StuckMoveThreshold = 0.2f; // metros mínimos para no considerarse atascado
        private const float StuckTimeout = 2.5f;       // tiempo máximo parado antes de ceder

        public MoveTask(Vector3 target, float speed) {
            _target = target;
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            // Condicionado a tener un punto válido y suficiente energía para el desplazamiento
            return _target != Vector3.zero && state.Energy >= CalculateEnergyCost(_speed);
        }

        public void ApplyEffects(WorldState state) {
            state.CurrentPosition = _target;
            state.Energy = Mathf.Max(0, state.Energy - CalculateEnergyCost(_speed));
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            
            if (!_isActionStarted) {
                actuators.SetSpeed(_speed);
                actuators.SetDestination(_target);
                _isActionStarted = true;
            }

            // --- Control de emisiones sonoras ---
            _noiseTimer -= Time.deltaTime;
            if (_noiseTimer <= 0f) {
                float noiseVolume = CalculateNoiseVolume(_speed);
                NoiseManager.EmitNoise(new NoiseEvent(state.CurrentPosition, noiseVolume, state.AgentName));
                _noiseTimer = StepInterval; 
            }

            // Verificación del final del trayecto
            if (!actuators.IsMoving()) {
                state.Energy = Mathf.Max(0, state.Energy - CalculateEnergyCost(_speed));
                return TaskExecutionStatus.Success;
            }

            return TaskExecutionStatus.Running;
        }

        // Escala el desgaste energético proporcional a la velocidad
        private float CalculateEnergyCost(float currentSpeed) {
            float t = Mathf.InverseLerp(1.0f, 6.5f, currentSpeed);
            return Mathf.Lerp(0.2f, 1f, t);
        }

        // Concreta cuan ruidosos son los pasos calculando en base a la velocidad
        private float CalculateNoiseVolume(float currentSpeed) {
            float t = Mathf.InverseLerp(3.0f, 6.5f, currentSpeed);
            return Mathf.Lerp(7f, 20f, t); 
        }
    }
}