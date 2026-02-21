using AgenticPrison.Core;
using AgenticPrison.Core.Math;
using System.Collections.Generic;

namespace AgenticPrison.Core {
    
    // OUTPUT: Actuators
    public interface IMovable {
        void SetDestination(Position3D target);
        void StopMoving();
        bool IsMoving();
        void SetSpeed(float speed);
    }

    // INPUT: Sensors
    public interface IVisualSensor {
        bool CheckFugitiveVisibility();
        Position3D? GetFugitivePosition();
    }

    public interface IHearingSensor {
    }

    public interface IGuardSensors {
        IVisualSensor Vision { get; }
        IHearingSensor Hearing { get; }
    }

    // INPUT: Static Spatial Knowledge (NUEVO)
    public interface IMapProvider {
        List<ZoneData> GetAllZones();
        ZoneData GetZone(string zoneId);
        float GetPathDistance(string fromZoneId, string toZoneId);
    }
}