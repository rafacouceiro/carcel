using UnityEngine;

namespace AgenticPrison.Core {
    
    // Interfaz para controlar el movimiento del agente en el entorno
    public interface IMovable {
        // Establece una posición destino en el mapa
        void SetDestination(Vector3 position);
        // Establece un objeto transform como destino a seguir
        void SetDestination(Transform target);
        // Detiene el movimiento actual del agente
        void StopMoving();
        // Indica si el agente se encuentra en movimiento
        bool IsMoving();
        // Modifica la velocidad de desplazamiento
        void SetSpeed(float speed);
        // Obtiene la rotación actual del agente
        float GetRotation();
        // Rota al agente hacia los grados especificados
        void RotateTo(float degrees);
    }

    // Interfaz para el control de la iluminación o señales visuales
    public interface ILightActuator {
        // Cambia el color del actuador de luz (como linterna o indicador)
        void SetLightColor(Color color);
    }

    // Agrupa todos los actuadores del agente para darlos a las tareas
    public interface IActuators : IMovable, ILightActuator { }
}