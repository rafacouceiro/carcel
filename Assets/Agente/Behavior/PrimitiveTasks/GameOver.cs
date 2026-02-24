using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class GameOverTask : IPrimitiveTask {
        
        private bool _hasTriggered = false;

        public bool CheckPreconditions(WorldState state) {
            // El TrapMethod ya hizo las matemáticas difíciles, así que si el 
            // planificador nos ha metido en la cola, damos luz verde.
            return true; 
        }

        public void ApplyEffects(WorldState state) {
            // Aquí en el futuro podrías poner algo como state.IsGameOver = true;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            if (!_hasTriggered) {
                // 1. Frenamos al guardia en seco
                actuators.SetSpeed(0f); 
                
                // 2. Anunciamos el fin del juego a lo grande
                Debug.Log("<color=red><b>¡FUGITIVO ATRAPADO! FIN DEL JUEGO.</b></color>");
                
                // 3. Pausamos el motor de físicas de Unity para congelar la escena
                Time.timeScale = 0f; 

                // TODO: En el futuro, aquí lanzarás la animación de "Arresto" 
                // o llamarás al GameManager para mostrar el menú de derrota.
                
                _hasTriggered = true;
            }

            // OJO AL TRUCO: Devolvemos "Running" en bucle infinito. 
            // ¿Por qué? Porque si devolvemos "Success", el HTN diría "¡Genial, tarea terminada!" 
            // y al fotograma siguiente intentaría volver a patrullar. 
            // En un Game Over, queremos que el cerebro de la IA se quede "congelado" aquí.
            return TaskExecutionStatus.Running; 
        }
    }
}