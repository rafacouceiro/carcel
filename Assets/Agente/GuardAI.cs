using UnityEngine;
using AgenticPrison.Physical;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Core.Math;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison {

    public class GuardAI : MonoBehaviour {
        [Header("Tangible References")]
        public Transform PlayerTarget;

        [Header("Logic State")]
        public WorldState CurrentState;

        private HTNPlanner _planner;
        private Queue<IPrimitiveTask> _currentPlan;
        private IPrimitiveTask _activeTask;

        private IMovable _movable; 
        private IVisualSensor _vision; 
        private IHearingSensor _hearing; 
        
        private ICompoundTask _rootTask;
        private bool _wasFugitiveInVision;

        private void Start() {

            CurrentState = new WorldState();
            CurrentState.MapKnowledge = MapManager.Instance;

            _planner = new HTNPlanner();
            _currentPlan = new Queue<IPrimitiveTask>();

            _movable = GetComponent<NavMeshDriver>();
            _vision = GetComponent<IVisualSensor>();
            // _hearing = GetComponent<IHearingSensor>();

            if (_vision is VisionSensor unityVision) {
                unityVision.Target = PlayerTarget;
            }

            _rootTask = new AgenticPrison.Behavior.CompoundTasks.RoutineTask();
        }

        private void Update() {
            UpdateSensors();
            ProcessHTNExecution();
        }

        private void UpdateSensors() {
            CurrentState.TimeDeltaContext = Time.deltaTime;
            CurrentState.CurrentTime = Time.time; // Inyección de tiempo real

            if (_vision != null) {
                _wasFugitiveInVision = CurrentState.FugitiveInVision;
                CurrentState.FugitiveInVision = _vision.CheckFugitiveVisibility();
                
                if (CurrentState.FugitiveInVision) {
                    CurrentState.LKP = _vision.GetFugitivePosition();
                }

                if (_wasFugitiveInVision != CurrentState.FugitiveInVision) {
                    // ForzarReplanificacion();
                }
            }
        }

        private void ForzarReplanificacion() {
            _currentPlan.Clear();
            _activeTask = null;
        }

        private void ProcessHTNExecution() {
            if (_rootTask == null) return;
            
            if (_currentPlan.Count == 0 && _activeTask == null) {

                Debug.LogWarning("No tengo plan, genero uno nuevo");
                _currentPlan = _planner.GeneratePlan(CurrentState, _rootTask);
                if (_currentPlan.Count > 0) _activeTask = _currentPlan.Dequeue();
            }

            if (_activeTask != null) {
                Debug.Log("Ejecutando tarea: " + _activeTask.GetType().Name);
                var status = _activeTask.Execute(_movable, CurrentState);

                if (status == TaskExecutionStatus.Success) {
                    _activeTask = (_currentPlan.Count > 0) ? _currentPlan.Dequeue() : null;
                } else if (status == TaskExecutionStatus.Failure) {
                    _currentPlan.Clear();
                    _activeTask = null;
                }
            }
        }
    }
}