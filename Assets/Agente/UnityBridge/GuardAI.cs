using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Interfaces;
using AgenticPrison.Core.Math;

namespace AgenticPrison.UnityBridge {

    /// <summary>
    /// Helper struct to bundle components implementing IActuators.
    /// </summary>
    public struct GuardActuators : IAgentActuators {
        public IMovable Movable { get; private set; }
        public IAnimatorControl Animator { get; private set; }

        public GuardActuators(IMovable movable, IAnimatorControl animator) {
            Movable = movable;
            Animator = animator;
        }
    }

    /// <summary>
    /// Core loop linking the Pure C# HTN logic to Unity's Update cycle.
    /// Acts as the execution driver.
    /// </summary>
    [RequireComponent(typeof(NavMeshDriver))]
    public class GuardAI : MonoBehaviour {
        
        [Header("State")]
        // Real WorldState updated continuously
        public WorldState CurrentState;

        private HTNPlanner _planner;
        private Queue<IPrimitiveTask> _currentPlan;
        private IPrimitiveTask _activeTask;

        private IAgentActuators _actuators;
        // private IGuardSensors _sensors; // Assign if sensor scripts are attached
        
        private ICompoundTask _rootTask;

        private void Start() {
            CurrentState = new WorldState();
            _planner = new HTNPlanner();
            _currentPlan = new Queue<IPrimitiveTask>();

            var navDriver = GetComponent<NavMeshDriver>();
            var animControl = GetComponent<IAnimatorControl>(); 
            
            _actuators = new GuardActuators(navDriver, animControl);

            // Dummy assignment until root behavior is added
            // _rootTask = new ...
        }

        private void Update() {
            UpdateSensors();
            ProcessHTNExecution();
        }

        private void UpdateSensors() {
            // Read from Unity components into CurrentState fields
            CurrentState.TimeDeltaContext = Time.deltaTime;
        }

        private void ProcessHTNExecution() {
            if (_rootTask == null) return; // Waiting for initialization
            
            if (_currentPlan.Count == 0 && _activeTask == null) {
                GenerateNewPlan();
            }

            if (_activeTask != null) {
                var status = _activeTask.Execute(_actuators, CurrentState);

                if (status == TaskExecutionStatus.Success) {
                    if (_currentPlan.Count > 0) {
                        _activeTask = _currentPlan.Dequeue();
                    } else {
                        _activeTask = null;
                    }
                } else if (status == TaskExecutionStatus.Failure) {
                    _currentPlan.Clear();
                    _activeTask = null;
                }
            }
        }

        public void GenerateNewPlan() {
            _currentPlan = _planner.GeneratePlan(CurrentState, _rootTask);
            if (_currentPlan != null && _currentPlan.Count > 0) {
                _activeTask = _currentPlan.Dequeue();
            } else {
                _activeTask = null; 
            }
        }
    }
}
