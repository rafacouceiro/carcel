using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Physical {
    
    public class VisionSensor : MonoBehaviour, IVisualSensor {
        [Header("Settings")]
        public float VisionRange = 15f;
        public float ViewAngle = 90f;
        public LayerMask ObstacleMask;

        // Esta propiedad la llenará el GuardAI
        public Transform Target { get; set; }

        public bool CheckFugitiveVisibility() {
            if (Target == null) return false; // Si no hay target, no hay visión

            Vector3 directionToTarget = (Target.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, Target.position);

            if (distanceToTarget > VisionRange) return false;
            if (Vector3.Angle(transform.forward, directionToTarget) > ViewAngle / 2) return false;

            if (Physics.Raycast(transform.position + Vector3.up, directionToTarget, out RaycastHit hit, VisionRange, ObstacleMask)) {
                if (hit.transform != Target) return false;
            }

            return true;
        }

        public Transform? GetFugitivePosition() {
            if (Target == null) return null;
            return Target;
        }
    }
}