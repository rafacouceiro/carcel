using AgenticPrison.Core;

namespace AgenticPrison.Agents.Camera {

    // La cámara no se mueve ni gasta energía, así que no necesita nada más
    // que lo que ya hereda de WorldState para coordinarse por FIPA.
    public class CameraWorldState : WorldState { }
}
