using AgenticPrison.Core;
using AgenticPrison.Core.Math;

namespace AgenticPrison.Interfaces {
    
    // OUTPUT: Actuators

    public interface IMovable {
        void SetDestination(Position3D target);
        void StopMoving();
        bool IsMoving();
        void SetSpeed(float speed);
    }

    public interface IAnimatorControl {
        void TriggerCatch();
        void TriggerInspect();
    }

    public interface IAgentActuators : IActuators {
        IMovable Movable { get; }
        IAnimatorControl Animator { get; }
    }

    // INPUT: Sensors

    public interface IVisualSensor {
        bool CheckFugitiveVisibility();
        Position3D? GetFugitivePosition();
        bool CheckPrisonerInCell();
    }

    public interface IHearingSensor {
        // Typically event driven (e.g., OnNoiseHeard), but exposed for any polling needs
    }

    public interface IGuardSensors {
        IVisualSensor Vision { get; }
        IHearingSensor Hearing { get; }
    }
}
