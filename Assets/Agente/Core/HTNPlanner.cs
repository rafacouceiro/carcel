using System.Collections.Generic;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using UnityEngine;

namespace AgenticPrison.Core {

    // Planificador HTN: Busca y genera planes estructurados basados en tareas y métodos
    public class HTNPlanner {
        
        // Genera un plan ejecutable (cola de primitivas) a partir de una tarea inicial
        public Queue<IPrimitiveTask> GeneratePlan(WorldState initialState, ICompoundTask rootTask) {
            var workingState = initialState.Clone();
            var finalPlan = new Queue<IPrimitiveTask>();
            
            var tasksToProcess = new Stack<ITask>();
            tasksToProcess.Push(rootTask);
            
            // Si logra construir un plan desde el estado inicial, lo retorna
            if (FindPlan(workingState, tasksToProcess, finalPlan)) {
                return finalPlan;
            }
            
            return new Queue<IPrimitiveTask>(); 
        }

        // Bucle recursivo central para la búsqueda HTN
        private bool FindPlan(WorldState state, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            // Si no quedan tareas por procesar, el plan está completo y es válido
            if (tasksToProcess.Count == 0) return true; 

            var currentTask = tasksToProcess.Pop();

            // Deriva la lógica en función del tipo de tarea (Compuesta o Primitiva)
            if (currentTask is ICompoundTask compoundTask) {
                return TryDecomposeCompound(state, compoundTask, tasksToProcess, finalPlan);
            } 
            else if (currentTask is IPrimitiveTask primitiveTask) {
                return TryProcessPrimitive(state, primitiveTask, tasksToProcess, finalPlan);
            }

            return false;
        }

        // Prueba todos los métodos de una tarea compuesta para intentar descomponerla
        private bool TryDecomposeCompound(WorldState state, ICompoundTask compoundTask, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            foreach (var method in compoundTask.Methods) {
                if (method.CheckPreconditions(state)) {
                    var subTasks = method.Decompose(state); 
                    
                    var clonedState = state.Clone();
                    
                    // Clona el stack para no afectar iteraciones fallidas
                    var tempStackArray = tasksToProcess.ToArray();
                    System.Array.Reverse(tempStackArray);
                    var temporaryStack = new Stack<ITask>(tempStackArray);
                    
                    // Añade las nuevas subtareas al stack de procesamiento
                    var subTaskArray = subTasks.ToArray();
                    for (int i = subTaskArray.Length - 1; i >= 0; i--) {
                        temporaryStack.Push(subTaskArray[i]);
                    }
                    
                    var branchPlan = new Queue<IPrimitiveTask>(finalPlan);

                    // Explora esta rama recursivamente
                    if (FindPlan(clonedState, temporaryStack, branchPlan)) {
                        finalPlan.Clear();
                        foreach(var t in branchPlan) finalPlan.Enqueue(t);
                        CopyState(clonedState, state); // Aplica estado simulado final
                        return true;
                    }
                }
            }
            return false;
        }

        // Simula la ejecución de una tarea primitiva verificando sus condiciones y efectos
        private bool TryProcessPrimitive(WorldState state, IPrimitiveTask primitiveTask, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            if (primitiveTask.CheckPreconditions(state)) {
                var clonedState = state.Clone(); 
                var branchPlan = new Queue<IPrimitiveTask>(finalPlan);
                
                var tempStackArray = tasksToProcess.ToArray();
                System.Array.Reverse(tempStackArray);
                var temporaryStack = new Stack<ITask>(tempStackArray);

                // Aplica los efectos al estado clonado
                primitiveTask.ApplyEffects(clonedState);
                branchPlan.Enqueue(primitiveTask); // Añade al plan en construcción

                // Continúa la búsqueda con el estado modificado
                if (FindPlan(clonedState, temporaryStack, branchPlan)) {
                    finalPlan.Clear();
                    foreach (var t in branchPlan) finalPlan.Enqueue(t);
                    CopyState(clonedState, state);
                    return true;
                }
            }
            return false;
        }

        // Copia el estado simulado en el estado original durante la construcción del plan
        private void CopyState(WorldState source, WorldState destination) {
            destination.FugitiveInVision      = source.FugitiveInVision;
            destination.seenByMe              = source.seenByMe;
            destination.PrisonerInCell        = source.PrisonerInCell;
            destination.Energy                = source.Energy;
            destination.LastKnownPosition     = source.LastKnownPosition;
            destination.CurrentPosition       = source.CurrentPosition;
            destination.Map                   = source.Map;
            destination.AssignedQuadrantId    = source.AssignedQuadrantId;
            destination.LastNoisePosition     = source.LastNoisePosition;
            destination.LastKnownPositionTime = source.LastKnownPositionTime;
            destination.LastNoisePositionTime = source.LastNoisePositionTime;
            destination.LastGuardPosition     = source.LastGuardPosition;
            destination.LastGuardPositionTime = source.LastGuardPositionTime;
            destination.AgentName             = source.AgentName;
            // Campos sociales Phase 2
            destination.AssignedTask          = source.AssignedTask;
            destination.TeamMembers           = new List<string>(source.TeamMembers);
            destination.ContractNetActive     = source.ContractNetActive;
            destination.PendingActions        = new Queue<ACLMessage>(source.PendingActions);
        }
    }
}