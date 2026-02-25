using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior;

namespace AgenticPrison {

    public class Brain : MonoBehaviour {
        [Header("Tangible References")]
        public Transform PlayerTarget;

        [Header("Configuración del Guardia")]
        [Tooltip("El ID simbólico del cuadrante. Puedes escribirlo o arrastrar el objeto abajo.")]
        public string QuadrantId = "section1"; // <--- Tu agente SOLO usará este string

// --- MAGIA DEL EDITOR DE UNITY ---
#if UNITY_EDITOR
        [Header("Herramientas de Editor (No se compila en el juego final)")]
        [Tooltip("Arrastra aquí un objeto. El agente copiará su nombre y soltará el objeto al instante.")]
        public Transform ArrastrarCuadrante;

        // OnValidate se ejecuta automáticamente cada vez que tocas algo en el Inspector
        private void OnValidate() {
            if (ArrastrarCuadrante != null) {
                QuadrantId = ArrastrarCuadrante.name; // 1. Copia el nombre al string
                ArrastrarCuadrante = null;            // 2. Borra la referencia física
            }
        }
#endif

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

            CurrentState.Map = PrisonMap.Instance;
            CurrentState.AssignedQuadrantId = QuadrantId;

            _planner = new HTNPlanner();
            _currentPlan = new Queue<IPrimitiveTask>();

            _movable = GetComponent<NavMeshDriver>();
            _vision = GetComponent<IVisualSensor>();
            // _hearing = GetComponent<IHearingSensor>();

            if (_vision is VisionSensor unityVision) {
                unityVision.Target = PlayerTarget;
            }

            _rootTask = new AgenticPrison.Behavior.RootTask.BeGuard();
        }

        private void Update() {
            UpdateSensors();
            ProcessHTNExecution();
        }

        // FUNCIONES SENSORIALES: ACTUALIZACIÓN DE ESTADO

        private void UpdateSensors() {
            UpdateVisionFugitive();
            UpdateLocation();
            // UpdateHearing();
        }

        /// <summary>
        /// Actualiza el estado de visión con respecto al fugitivo
        /// forzando la replanificaicon si es necesario
        /// </summary>
        private void UpdateVisionFugitive() {

            if (_vision != null) {

                bool wasFugitiveInVision = CurrentState.FugitiveInVision;
                CurrentState.FugitiveInVision = _vision.CheckFugitiveVisibility();

                if (CurrentState.FugitiveInVision) {
                    Debug.LogWarning("Veo al fugitivo");
                    CurrentState.LastKnownPosition = _vision.GetFugitivePosition();
                    CurrentState.Alertness = true; // Entra en estado de alerta al ver al fugitivo
                }
                
                // Si vemos / perdemos de vista al fugitivo replanificamos 
                if (wasFugitiveInVision != CurrentState.FugitiveInVision) {
                    ForzarReplanificacion();
                }
            }
        }

        private void UpdateLocation(){
            CurrentState.CurrentPosition = transform.position;
        }

        private void ForzarReplanificacion() {

            _currentPlan.Clear();
            _activeTask = null;    
            _movable.StopMoving();
        }

        private void ProcessHTNExecution() {

            if (_rootTask == null) return;
            
            // Si no tenemos plan: generar uno
            if (_currentPlan.Count == 0 && _activeTask == null) {

                _currentPlan = _planner.GeneratePlan(CurrentState, _rootTask);
                if (_currentPlan.Count > 0) _activeTask = _currentPlan.Dequeue(); // Comenzar plan
            }

            // Si hay una tarea en eejcución
            if (_activeTask != null) {
                var status = _activeTask.Execute(_movable, CurrentState); // Ejecutarla (proporcionar actuadores)

                if (status == TaskExecutionStatus.Success) { // Si se completa con éxito
                    _activeTask = (_currentPlan.Count > 0) ? _currentPlan.Dequeue() : null; // Siguiente tarea
                } else if (status == TaskExecutionStatus.Failure) { // Si falla
                    _currentPlan.Clear(); // Limpiar plan
                    _activeTask = null; // Resetear, va a replanificar en el siguiente frame
                }
            }
        }
    }
}