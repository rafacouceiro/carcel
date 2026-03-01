using System.Collections.Generic;
using AgenticPrison.Physical;

namespace AgenticPrison.Core {

    public class HTNPlanner {
        
        public Queue<IPrimitiveTask> GeneratePlan(WorldState initialState, ICompoundTask rootTask) {
            var workingState = initialState.Clone();
            var finalPlan = new Queue<IPrimitiveTask>();
            
            var tasksToProcess = new Stack<ITask>();
            tasksToProcess.Push(rootTask);
            
            if (FindPlan(workingState, tasksToProcess, finalPlan)) {
                return finalPlan;
            }
            
            return new Queue<IPrimitiveTask>(); 
        }

        private bool FindPlan(WorldState state, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            if (tasksToProcess.Count == 0) return true; 

            var currentTask = tasksToProcess.Pop();

            if (currentTask is ICompoundTask compoundTask) {
                return TryDecomposeCompound(state, compoundTask, tasksToProcess, finalPlan);
            } 
            else if (currentTask is IPrimitiveTask primitiveTask) {
                return TryProcessPrimitive(state, primitiveTask, tasksToProcess, finalPlan);
            }

            return false;
        }

        private bool TryDecomposeCompound(WorldState state, ICompoundTask compoundTask, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            foreach (var method in compoundTask.Methods) {
                if (method.CheckPreconditions(state)) {
                    var subTasks = method.Decompose(state); 
                    
                    var clonedState = state.Clone();
                    
                    var tempStackArray = tasksToProcess.ToArray();
                    System.Array.Reverse(tempStackArray);
                    var temporaryStack = new Stack<ITask>(tempStackArray);
                    
                    var subTaskArray = subTasks.ToArray();
                    for (int i = subTaskArray.Length - 1; i >= 0; i--) {
                        temporaryStack.Push(subTaskArray[i]);
                    }
                    
                    var branchPlan = new Queue<IPrimitiveTask>(finalPlan);

                    if (FindPlan(clonedState, temporaryStack, branchPlan)) {
                        finalPlan.Clear();
                        foreach(var t in branchPlan) finalPlan.Enqueue(t);
                        CopyState(clonedState, state);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryProcessPrimitive(WorldState state, IPrimitiveTask primitiveTask, Stack<ITask> tasksToProcess, Queue<IPrimitiveTask> finalPlan) {
            if (primitiveTask.CheckPreconditions(state)) {
                var clonedState = state.Clone(); 
                var branchPlan = new Queue<IPrimitiveTask>(finalPlan);
                
                var tempStackArray = tasksToProcess.ToArray();
                System.Array.Reverse(tempStackArray);
                var temporaryStack = new Stack<ITask>(tempStackArray);

                primitiveTask.ApplyEffects(clonedState);
                branchPlan.Enqueue(primitiveTask);

                if (FindPlan(clonedState, temporaryStack, branchPlan)) {
                    finalPlan.Clear();
                    foreach (var t in branchPlan) finalPlan.Enqueue(t);
                    CopyState(clonedState, state);
                    return true;
                }
            }
            return false;
        }

        private void CopyState(WorldState source, WorldState destination) {
            destination.FugitiveInVision = source.FugitiveInVision;
            destination.PrisonerInCell = source.PrisonerInCell;
            destination.Energy = source.Energy;
            destination.LastKnownPosition = source.LastKnownPosition;
            destination.CurrentPosition = source.CurrentPosition;
            destination.Map = source.Map;
            destination.AssignedQuadrantId = source.AssignedQuadrantId;
            destination.LastNoisePosition = source.LastNoisePosition;
            destination.LastKnownPositionTime = source.LastKnownPositionTime;
            destination.LastNoisePositionTime = source.LastNoisePositionTime;
            destination.LastGuardPosition = source.LastGuardPosition;
            destination.LastGuardPositionTime = source.LastGuardPositionTime;
            destination.AgentName = source.AgentName;
        }
    }
}