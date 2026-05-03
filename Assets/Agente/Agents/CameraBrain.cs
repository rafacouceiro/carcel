using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Agents {

    [RequireComponent(typeof(CameraFSM))]
    public class CameraBrain : MonoBehaviour, IVisionEvents {

        static int cameraCounter = 1;

        CameraFSM _fsm;

        [SerializeField] private Light spotLight;

        private void Awake() {
            gameObject.name = "Camara" + cameraCounter++;
        }

        private void Start() {
            _fsm = GetComponent<CameraFSM>();

            if (spotLight != null)
                spotLight.enabled = false;
        }

        public void OnFugitiveSpotted(Vector3 position) {
            Debug.Log($"<color=red>{gameObject.name.ToUpper()}: FUGITIVO DETECTADO EN POSICIÓN {position}</color>");

            if (spotLight != null)
                StartCoroutine(FlashLight());

            List<string> sectors = PrisonMap.Instance.GetFugitiveSectors(position);
            string sectorId = sectors != null && sectors.Count == 1 ? sectors[0] : "[UNK]";

            _fsm.NotifyFugitiveSpotted(position, sectorId);
        }

        private IEnumerator FlashLight() {
            spotLight.enabled = true;
            spotLight.intensity = 600f;
            spotLight.range = 50f;

            yield return new WaitForSeconds(1.5f);

            spotLight.enabled = false;
        }

        public void OnFugitiveLost()                          { }
        public void OnFugitivePositionUpdated(Vector3 pos)    { }
        public void OnGuardSpotted(Vector3 guardPosition)     { }
    }
}