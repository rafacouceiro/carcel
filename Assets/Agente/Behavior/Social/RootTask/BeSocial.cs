using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea raíz del plano social del guardia.
    // Selecciona cada frame entre: iniciar un protocolo, responder mensajes pendientes, o esperar.
    public class BeSocial : ICompoundTask {
        public List<IMethod> Methods { get; }

        public BeSocial(FIPAAgent agent, float contractNetReplyWindow) {
            Methods = new List<IMethod> {
                new DissolveTeamMethod(agent),                       // disolver equipo cuando sweep completo
                new GenerateProtocolMethod(agent, contractNetReplyWindow),
                new SendResponseMethod(agent),
                new SocialIdleMethod()
            };
        }
    }
}
