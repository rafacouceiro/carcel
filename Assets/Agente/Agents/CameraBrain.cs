/*
using UnityEngine;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Agents {

    // Cerebro de la cámara: Solo procesa eventos de visión específicos
    public class CameraBrain : FIPAAgent, IVisionEvents {

        public override string AgentId => gameObject.name;

        [Header("Estado")]
        public bool IsDetectingFugitive = false;
        
        // La cámara necesita un estado aunque no lo use para HTN complejo
        private WorldState _dummyState = new WorldState();

        protected override void Start() {
            base.Start();
        }

        protected override void Update() {
            base.Update();
            // Procesamos la radio de la cámara (para recibir alertas si fuera necesario)
            ProcessIncoming(_dummyState);
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

        protected override void OnMessageReceived(ACLMessage msg, WorldState ws) { }
    }
}*/