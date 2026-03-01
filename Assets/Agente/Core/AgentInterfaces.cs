using UnityEngine;

namespace AgenticPrison.Core {
    
    // Interfaz dedicada exclusivamente a la locomoción
    public interface IMovable {
        void SetDestination(Vector3 position);
        void SetDestination(Transform target);
        void StopMoving();
        bool IsMoving();
        void SetSpeed(float speed);
        float GetRotation();
        void RotateTo(float degrees);
    }

    // Interfaz dedicada exclusivamente a la señalización visual
    public interface ILightActuator {
        void SetLightColor(Color color);
    }

    /// <summary>
    /// Interfaz superior que agrupa todos los canales de salida del agente.
    /// Las tareas primitivas recibirán esta interfaz permitiendo acceso a ambos sistemas.
    /// </summary>
    public interface IActuators : IMovable, ILightActuator { }
}