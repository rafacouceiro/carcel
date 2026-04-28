using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea compuesta: decide qué protocolo iniciar según el estado del mundo.
    public class GenerateProtocol : ICompoundTask {
        public List<IMethod> Methods { get; }

        public GenerateProtocol(FIPAAgent agent) {
            Methods = new List<IMethod> {
                new InitiateContractNetMethod(agent),
                new LaunchQueryMethod(agent)                // verifica ruidos antes de investigar
            };
        }
    }
}