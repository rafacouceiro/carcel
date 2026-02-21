using UnityEngine;
using AgenticPrison.Physical;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Core.Math;

namespace AgenticPrison {

    public class GuardAI : MonoBehaviour {
        [Header("Tangible References")]
        public Transform PlayerTarget;

        [Header("Logic State")]
        public WorldState CurrentState;

        private HTNPlanner _planner;
        private Queue<IPrimitiveTask> _currentPlan;
        private IPrimitiveTask _activeTask;

        // SENSORES Y ACTUADORES DIRECTOS (Sin envoltorios extra)
        private IMovable _movable; // Actuador de movimiento
        private IVisualSensor _vision; // Sentido de la vista
        private IHearingSensor _hearing; // Sentido del oído
        
        private ICompoundTask _rootTask;

        // Variable para detectar cambios
        private bool _wasFugitiveInVision;

        private void Start() {
            CurrentState = new WorldState();
            _planner = new HTNPlanner();
            _currentPlan = new Queue<IPrimitiveTask>();

            // 1. Obtener componentes de Unity directamente
            _movable = GetComponent<NavMeshDriver>();
            _vision = GetComponent<IVisualSensor>();
            _hearing = GetComponent<IHearingSensor>();

            // 2. Inyectar el objetivo en la visión si es el sensor de Unity
            if (_vision is VisionSensor unityVision) {
                unityVision.Target = PlayerTarget;
            }

            // _rootTask = new GuardRootTask(); // Punto de entrada
        }

        private void Update() {
            UpdateSensors();
            ProcessHTNExecution();
        }

        private void UpdateSensors() {
            
            CurrentState.TimeDeltaContext = Time.deltaTime;

            if (_vision != null) {
                // 1. Guardamos el estado actual antes de actualizarlo
                _wasFugitiveInVision = CurrentState.FugitiveInVision;

                // 2. Actualizamos el estado con el sensor
                CurrentState.FugitiveInVision = _vision.CheckFugitiveVisibility();
                
                if (CurrentState.FugitiveInVision) {
                    CurrentState.LKP = _vision.GetFugitivePosition();
                }

                // 3. COMPROBACIÓN CRÍTICA: ¿Ha cambiado la visibilidad?
                if (_wasFugitiveInVision != CurrentState.FugitiveInVision) {
                    Debug.Log("Cambio de visibilidad detectado. Replanificando...");
                    ForzarReplanificacion();
                }
            }
        }

        // Un pequeño método auxiliar para limpiar el plan actual
        private void ForzarReplanificacion() {
            _currentPlan.Clear();
            _activeTask = null;
        }

        private void ProcessHTNExecution() {
            if (_rootTask == null) return;
            
            if (_currentPlan.Count == 0 && _activeTask == null) {
                _currentPlan = _planner.GeneratePlan(CurrentState, _rootTask);
                if (_currentPlan.Count > 0) _activeTask = _currentPlan.Dequeue();
            }

            if (_activeTask != null) {
                // Pasamos el _movable (que es un IActuators) directamente
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