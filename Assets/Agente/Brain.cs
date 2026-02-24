using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior;

namespace AgenticPrison {

    public class Brain : MonoBehaviour {
        [Header("Tangible References")]
        public Transform PlayerTarget;
        [Tooltip("Arrastra aquí el objeto padre del cuadrante (ej: section1)")]
        public Transform QuadrantRoot;

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

            // --- INICIO INTEGRACIÓN DEL CUADRANTE ---
            if (QuadrantRoot != null) {
                // Buscamos todas las salas hijas del objeto section1
                RoomNode[] roomsInQuadrant = QuadrantRoot.GetComponentsInChildren<RoomNode>();
                CurrentState.AssignedQuadrant = new List<RoomNode>(roomsInQuadrant);
            } else {
                Debug.LogWarning($"[Brain] El agente {gameObject.name} no tiene un QuadrantRoot asignado.");
            }

            // Calculamos en qué sala nace el agente para inicializar su memoria espacial
            CurrentState.CurrentRoomNode = GetSpawnRoom(CurrentState.AssignedQuadrant);
            // --- FIN INTEGRACIÓN DEL CUADRANTE ---

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

            if (_vision != null) {
                _wasFugitiveInVision = CurrentState.FugitiveInVision;
                CurrentState.FugitiveInVision = _vision.CheckFugitiveVisibility();
                
                if (CurrentState.FugitiveInVision) {
                    // TODO: Actualizar estado del fugitivo
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

        // --- FUNCIONES AUXILIARES DE NAVEGACIÓN ---

        private RoomNode GetSpawnRoom(List<RoomNode> rooms) {
            if (rooms == null || rooms.Count == 0) return null;

            RoomNode closest = null;
            float minDistance = Mathf.Infinity;

            foreach (var room in rooms) {
                BoxCollider col = room.GetComponent<BoxCollider>();
                if (col != null) {
                    float dist = Vector3.Distance(transform.position, col.bounds.center);
                    if (dist < minDistance) {
                        minDistance = dist;
                        closest = room;
                    }
                }
            }
            return closest;
        }
    }
}