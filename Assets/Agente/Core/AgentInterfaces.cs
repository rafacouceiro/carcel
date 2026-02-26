using AgenticPrison.Core;
using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Core {
    
    public interface IMovable {
        void SetDestination(Vector3 position);
        void SetDestination(Transform target);
        void StopMoving();
        bool IsMoving();
        void SetSpeed(float speed);
        float GetRotation();
        void RotateTo(float degrees);
        void SetLightColor(Color color);
    }

    // INPUT: Sensors
    public interface IVisualSensor {
        bool CheckFugitiveVisibility();
        Vector3 GetFugitivePosition();
    }

    public interface IGuardSensors {
        IVisualSensor Vision { get; }
    }

}