using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class MoveTask : IPrimitiveTask {
        
        private Vector3 _target;
        private float _speed;
        private bool _isActionStarted = false;

        // El Método que crea esta tarea le pasa el destino y la velocidad
        public MoveTask(Vector3 target, float speed) {
            _target = target;
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            return _target != Vector3.zero && state.Energy >= CalculateEnergyCost(_speed);
        }

        public void ApplyEffects(WorldState state) {
            // Actualizamos la posicion del agente
            state.CurrentPosition = _target;
            state.Energy = Matf.Max(0, state.Energy - CalculateEnergyCost(_speed));
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // Mandar la orden al NavMesh solo el primer fotograma
            if (!_isActionStarted) {
                actuators.SetSpeed(_speed);
                actuators.SetDestination(_target);
                _isActionStarted = true;
            }

            // Si el NavMesh dice que ya llegamos, la tarea fue un éxito
            if (!actuators.IsMoving()) {
                // Aplicamos el gasto energético
                state.Energy = Mathf.Max(0, state.Energy - CalculateEnergyCost(_speed));
                return TaskExecutionStatus.Success;
            }

            // Mientras tanto, seguimos corriendo la tarea
            return TaskExecutionStatus.Running;
        }

        /// <summary>
        /// Gasto lineal: 2.5 de velocidad gasta 1 punto. 6.5 gasta 5 puntos.
        /// </summary>
        private float CalculateEnergyCost(float currentSpeed) {
            float t = Mathf.InverseLerp(3.0f, 6.5f, currentSpeed);
            return Mathf.Lerp(1f, 5f, t);
        }
    }
}