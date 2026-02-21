using AgenticPrison.Core;
using AgenticPrison.Core.Math;

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

    // Map info
    public interface IMapProvider {
        List<ZoneData> GetAllZones();
        ZoneData GetZone(string zoneId);
        
        // NUEVO: El cerebro pide la distancia real de navegación
        float GetPathDistance(string fromZoneId, string toZoneId);
    }

    public interface IHearingSensor {
        // Typically event driven (e.g., OnNoiseHeard), but exposed for any polling needs
    }

    public interface IGuardSensors {
        IVisualSensor Vision { get; }
        IHearingSensor Hearing { get; }
    }
}
