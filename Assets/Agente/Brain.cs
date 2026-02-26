using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior;
#if UNITY_EDITOR
using UnityEditor; // Necesario para Handles
#endif

namespace AgenticPrison {

    public class Brain : MonoBehaviour, INoiseReceiver, IVisionEvents {

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

        [Header("Audición")]
        public float AuditoryRange = 20f;

        private HTNPlanner _planner;
        private Queue<IPrimitiveTask> _currentPlan;
        private IPrimitiveTask _activeTask;

        private IMovable _movable; 
        private ICompoundTask _rootTask;

        private void Start() {

            CurrentState = new WorldState();

            CurrentState.Map = PrisonMap.Instance;
            CurrentState.AssignedQuadrantId = QuadrantId;

            _planner = new HTNPlanner();
            _currentPlan = new Queue<IPrimitiveTask>();

            _movable = GetComponent<NavMeshDriver>();

            _rootTask = new AgenticPrison.Behavior.RootTask.BeGuard();
        }

        private void Update() {
            UpdateLocation();
            ProcessHTNExecution();
        }

        public Vector3 GetPosition() {
            return transform.position;
        }

        private void OnEnable() {
            NoiseManager.RegisterReceiver(this);
        }

        private void OnDisable() {
            NoiseManager.UnregisterReceiver(this);
        }

        // EVENTOS DE AUDICIÓN
        public void OnNoiseHeard(NoiseEvent noise) 
        {
            float dist = Vector3.Distance(transform.position, noise.Position);

            // Cálculo de intensidad
            float intensity = 1f - (dist / noise.Volume);

            Debug.LogWarning($"<color=cyan>Escucho ruido con intensidad: {intensity}</color>");
            
            if (intensity > 0.1f)
            {
                float errorMagnitude = Mathf.Lerp(0.5f, 5f, dist / noise.Volume);
                Vector2 randomCircle = Random.insideUnitCircle * errorMagnitude;
                Vector3 diffusePosition = noise.Position + new Vector3(randomCircle.x, 0, randomCircle.y);

                CurrentState.LastNoisePosition = diffusePosition;

                ForzarReplanificacion();
            }
        }

        // EVENTOS DE VISION
        public void OnFugitiveSpotted(Vector3 position) {
            CurrentState.FugitiveInVision = true;
            CurrentState.LastKnownPosition = position;
            ForzarReplanificacion(); 
        }

        public void OnFugitivePositionUpdated(Vector3 position) {
            CurrentState.LastKnownPosition = position;
        }

        public void OnFugitiveLost() {
            CurrentState.FugitiveInVision = false;
            ForzarReplanificacion();
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