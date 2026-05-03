using UnityEngine;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents {

    // Cerebro de la cámara: Solo procesa eventos de visión específicos
    public class CameraBrain : FIPAAgent, IVisionEvents {

        public override string AgentId => gameObject.name;

        [Header("Estado")]
        public bool IsDetectingFugitive = false;

        protected override void Start() {
            // Registro en el MessageBus (FIPA) por si en el futuro debe enviar mensajes
            base.Start();
        }

        protected override void Update() {
            base.Update();
            // Anunciamos la presencia de la cámara en el sistema de visión
            // Esto permite que el VisionManager sepa que este objeto "existe"
            VisionManager.EmitPresence(this.transform);
        }

        // --- IMPLEMENTACIÓN IVISIONEVENTS (Solo reacción al preso) ---

        public void OnFugitiveSpotted(Vector3 position) {
            IsDetectingFugitive = true;
            Debug.Log($"<color=red>[CÁMARA {AgentId}] INTRAUSO DETECTADO en la posición: {position}</color>");
            
            // Aquí podrías en el futuro enviar un ACLMessage a los guardias
        }

        public void OnFugitivePositionUpdated(Vector3 position) {
            // Log opcional para seguimiento constante
            // Debug.Log($"[CÁMARA {AgentId}] Actualizando posición del fugitivo: {position}");
        }

        public void OnFugitiveLost() {
            IsDetectingFugitive = false;
            Debug.Log($"<color=white>[CÁMARA {AgentId}] Objetivo perdido. Restableciendo vigilancia.</color>");
        }

        public void OnGuardSpotted(Vector3 guardPosition) {
            // La cámara es "inteligente": ignora a los guardias para no saturar la consola
            // No implementamos lógica aquí.
        }

        // Requerido por FIPA pero no usado activamente por ahora
        public override string[] GetOntologies() => new string[0];
        protected override void OnMessageReceived(ACLMessage msg) { }
    }
}