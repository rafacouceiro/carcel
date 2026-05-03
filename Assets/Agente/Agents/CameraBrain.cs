using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Agents {

    // Capa física de la cámara de vigilancia.
    //
    // Responsabilidad exclusiva: recibir eventos del sensor de visión y traducirlos
    // al lenguaje de CameraFSM. No contiene lógica de coordinación ni de protocolo.
    [RequireComponent(typeof(CameraFSM))]
    public class CameraBrain : MonoBehaviour, IVisionEvents {

        static int cameraCounter = 1;

        CameraFSM _fsm;

        private void Awake() {
            gameObject.name = "Camara" + cameraCounter++;
        }

        private void Start() {
            _fsm = GetComponent<CameraFSM>();
        }

        // ── IVisionEvents ─────────────────────────────────────────────────────────

        public void OnFugitiveSpotted(Vector3 position) {
            Debug.Log($"<color=red>{gameObject.name.ToUpper()}: FUGITIVO DETECTADO EN POSICIÓN {position}</color>");

            List<string> sectors = PrisonMap.Instance.GetFugitiveSectors(position);
            string sectorId = sectors != null && sectors.Count == 1 ? sectors[0] : "[UNK]";

            _fsm.NotifyFugitiveSpotted(position, sectorId);
        }

        public void OnFugitiveLost()                          { }
        public void OnFugitivePositionUpdated(Vector3 pos)    { }
        public void OnGuardSpotted(Vector3 guardPosition)     { }
    }
}
