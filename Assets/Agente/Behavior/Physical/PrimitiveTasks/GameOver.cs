using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: El agente ha atrapado al fugitivo, terminando la simulación
    public class GameOverTask : IPrimitiveTask {
        
        private bool _hasTriggered = false;

        public bool CheckPreconditions(WorldState state) {
            // El método superior (TrapMethod) asegura la precondición matemática para el éxito
            return true; 
        }

        public void ApplyEffects(WorldState state) {
            // Sin efectos de estado adicionales necesarios
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            
            if (!_hasTriggered) {
                // Detener el movimiento físico de forma abrupta
                actuators.SetSpeed(0f); 
                
                // Anunciar evento crítico
                Debug.Log("<color=red><b>¡FUGITIVO ATRAPADO! FIN DEL JUEGO.</b></color>");
                
                // Congela la lógica de Unity
                Time.timeScale = 0f; 
                
                _hasTriggered = true;
            }

            // Devuelve Running de forma continua para bloquear la asignación 
            // de tareas futuras por el planificador y paralizar al agente
            return TaskExecutionStatus.Running; 
        }
    }
}