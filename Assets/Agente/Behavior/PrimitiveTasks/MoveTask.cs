using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class MoveTask : IPrimitiveTask {
        
        private Transform _target;
        private float _speed;
        private bool _isActionStarted = false;

        // --- LA MAGIA ESTÁ AQUÍ ---
        // El Método que crea esta tarea le pasa el destino y la velocidad
        public MoveTask(Transform target, float speed) {
            _target = target;
            _speed = speed;
        }

        public bool CheckPreconditions(WorldState state) {
            return _target != null;
        }

        public void ApplyEffects(WorldState state) {
            // Un pequeño desgaste físico por cada punto al que caminamos
            state.Fatigue += 0.01f; 

            // Mayor fatiga si corremos
            if (_speed > 3.5f){
                state.Fatigue += 0.05f;
            }
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            // 1. Mandar la orden al NavMesh solo el primer fotograma
            if (!_isActionStarted) {
                actuators.SetSpeed(_speed);
                actuators.SetDestination(_target);
                _isActionStarted = true;
            }

            // 2. Si el NavMesh dice que ya llegamos, la tarea fue un éxito
            if (!actuators.IsMoving()) {
                return TaskExecutionStatus.Success;
            }

            // 3. Mientras tanto, seguimos corriendo la tarea
            return TaskExecutionStatus.Running;
        }
    }
}