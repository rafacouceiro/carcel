using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior;
#if UNITY_EDITOR
using UnityEditor; // Necesario para Handles
#endif

namespace AgenticPrison {

    public class Brain : MonoBehaviour, INoiseReceiver, IVisionEvents, ICellEventReceiver {

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

        [Tooltip("El nombre del agente, recogido automáticamente del GameObject.")]
        public string AgentName;
        private static int _guardCounter = 1;

        private void Awake() {
            AgentName = "Patrulla" + _guardCounter;
            gameObject.name = AgentName;
            _guardCounter++; 
        }

        private void Start() {

            CurrentState = new WorldState();

            CurrentState.AgentName = AgentName;
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

            if (noise.emisor == AgentName) return; // Ignorar ruido propio 

            // Ignorar ruido si tenemos al fugitivo en visión
            if (CurrentState.FugitiveInVision) 
            {
                return;
            }

            float dist = Vector3.Distance(transform.position, noise.Position);
            float errorMagnitude = Mathf.Lerp(0.5f, 10f, dist / noise.Volume);
            Vector2 randomCircle = Random.insideUnitCircle * errorMagnitude;
            Vector3 diffusePosition = noise.Position + new Vector3(randomCircle.x, 0, randomCircle.y);


            // Si tengo una pista visual reciente, ignoro el ruido
            if (CurrentState.LastKnownPosition != Vector3.zero)
            {    
                CurrentState.LastNoisePosition = diffusePosition;
                CurrentState.LastNoisePositionTime = Time.time;        
                return;
            } 
            else if (CurrentState.LastNoisePosition != Vector3.zero)
            {
                // Solamente replanificamos si el sonido escuchado es muy fuerte
                if (noise.Volume > 18f && Vector3.Distance(CurrentState.LastNoisePosition, diffusePosition) > 15f)
                {
                    CurrentState.LastNoisePosition = diffusePosition;
                    CurrentState.LastNoisePositionTime = Time.time;
                    ForzarReplanificacion();
                }
                // Si es cualquier otro sonido seguimos investigando
                else
                {
                    return;
                }
            }
            // Si no tenemos pistas visuales o auditivas recientes 
            // reaccionamos al sonido
            else
            {
                ForzarReplanificacion();
            }            
        }

        // EVENTOS DE VISION
        public void OnFugitiveSpotted(Vector3 position) {

             Debug.LogWarning($"<color=magenta>{CurrentState.PrisonerInCell} prisioner in cell</color>");

            // Para la primera vez que lo vemos: queremos saber si esá en la celda o fuera
            // Si está dentro de la celda, ignoramos que hemos visto al fugitivo
            if(CurrentState.PrisonerInCell)
            {
                List<WayPointData> cellPoints = CurrentState.Map.GetAllCellPoints();
                bool isInsideAnyCell = false;

                foreach(WayPointData cellPoint in cellPoints)
                {
                    BoxCollider cellBox = cellPoint.GetComponent<BoxCollider>();
                    if(cellBox != null)
                    {
                        if(cellBox.bounds.Contains(position))
                        {
                            isInsideAnyCell = true;
                            break; // Ya sabemos que está en una celda, dejamos de buscar
                        }
                    }
                }
                if(isInsideAnyCell)
                {
                    Debug.LogWarning("<color=magenta>El prisionero está dentro de la celda.</color>");
                    return; 
                }
            }
            
            Debug.LogWarning("<color=red>He visto al prisionero fuera de la celda");
            CurrentState.PrisonerInCell = false;
            CurrentState.FugitiveInVision = true;
            CurrentState.LastKnownPosition = position;
            CurrentState.LastKnownPositionTime = Time.time;
            ForzarReplanificacion(); 
        }

        public void OnFugitivePositionUpdated(Vector3 position) {
            if (CurrentState.PrisonerInCell) return; // Ignorar si el prisionero está en la celda
            CurrentState.LastKnownPosition = position;
            CurrentState.LastKnownPositionTime = Time.time;
        }

        public void OnFugitiveLost() {
            Debug.LogWarning("<color=red>He perdido de vista al prisionero");
            CurrentState.FugitiveInVision = false;
        }

        public void OnCellFoundOpen() 
        {
            if (CurrentState.PrisonerInCell) {
                CurrentState.PrisonerInCell = false;  
                Debug.LogWarning("<color=yellow>El prisionero SE HA FUGADO");              
                ForzarReplanificacion();
            }
        }

        // Actualizar posición del agente
        private void UpdateLocation(){
            CurrentState.CurrentPosition = transform.position;
        }


        private void ForzarReplanificacion() {

            _currentPlan.Clear();
            _activeTask = null;    
            _movable.StopMoving();
        }

        // Flujo de HTN
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