namespace AgenticPrison.Communication {

    // Performativas FIPA-ACL soportadas por el sistema de comunicación
    public enum Performative {
        Cfp,
        Propose,
        Refuse,
        AcceptProposal,
        RejectProposal,
        InformDone,
        InformResult,
        Failure,
        Cancel,
        NotUnderstood,
        Inform
    }
}
