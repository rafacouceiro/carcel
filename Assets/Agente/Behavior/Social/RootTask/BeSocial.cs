using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea raíz del HTN social. Cada frame decide si lanzar un protocolo o no hacer nada.
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
