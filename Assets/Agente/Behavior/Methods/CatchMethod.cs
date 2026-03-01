using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Lógica para decidir si el guardia puede atrapar al fugitivo inmediatamente
    public class CatchMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            float distance = GetDistanceToTarget(state.CurrentPosition, state.LastKnownPosition);
            // Solo es posible si lo está viendo y lo tiene al alcance de la mano
            return state.FugitiveInVision && distance < 1.5f;
        }

        private float GetDistanceToTarget(Vector3 agentPosition, Vector3 targetPosition) {
            return Vector3.Distance(agentPosition, targetPosition);
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            // Descompone en la acción terminal del juego
            subTasks.Enqueue(new GameOverTask());
            return subTasks;
        }
    }
}