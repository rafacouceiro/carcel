using System.Collections.Generic;

namespace AgenticPrison.Communication {

    // Registro de todas las conversaciones del sistema para logging y diagnóstico.
    // Las conversaciones completadas NO se eliminan automáticamente.
    public class ConversationTracker {

        static ConversationTracker _instance;
        public static ConversationTracker Instance => _instance ?? (_instance = new ConversationTracker());

        readonly List<ConversationRecord> _records = new List<ConversationRecord>();

        ConversationTracker() { }

        public void Register(string id, string initiator) {
            _records.Add(new ConversationRecord {
                Id        = id,
                Initiator = initiator,
                StartTime = UnityEngine.Time.time,
                State     = "CfpSent",
                Outcome   = "Active"
            });
        }

        public void AddParticipant(string id, string participant) {
            ConversationRecord r = Find(id);
            if (r != null) r.Participants.Add(participant);
        }

        public void UpdateState(string id, string state) {
            ConversationRecord r = Find(id);
            if (r != null) r.State = state;
        }

        public void SetOutcome(string id, string outcome) {
            ConversationRecord r = Find(id);
            if (r == null) return;
            r.Outcome  = outcome;
            r.EndTime  = UnityEngine.Time.time;
        }

        ConversationRecord Find(string id) {
            foreach (ConversationRecord r in _records)
                if (r.Id == id) return r;
            return null;
        }
    }

    public class ConversationRecord {
        public string       Id;
        public string       Initiator;
        public List<string> Participants = new List<string>();
        public string       State;       // representación del estado FSM actual
        public float        StartTime;
        public float        EndTime;     // 0 mientras está activa
        public string       Outcome;     // "Active", "Done", "Failed"
    }
}
