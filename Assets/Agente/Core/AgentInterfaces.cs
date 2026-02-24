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
    }

    // INPUT: Sensors
    public interface IVisualSensor {
        bool CheckFugitiveVisibility();
        Vector3 GetFugitivePosition();
    }

    public interface IHearingSensor {
    }

    public interface IGuardSensors {
        IVisualSensor Vision { get; }
        IHearingSensor Hearing { get; }
    }

}