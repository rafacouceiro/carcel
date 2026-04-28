using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea raíz del plano social del guardia.
    // Selecciona cada frame entre: iniciar un protocolo o esperar.
    // La disolución de equipo y la respuesta a CFPs ahora son reactivas (canal + FIPAAgent).
    public class BeSocial : ICompoundTask {
        public List<IMethod> Methods { get; }

        public BeSocial(FIPAAgent agent) {
            Methods = new List<IMethod> {
                new GenerateProtocolMethod(agent),
                new SocialIdleMethod()
            };
        }
    }
}
