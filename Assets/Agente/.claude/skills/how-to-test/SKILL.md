---
name: how-to-test
description: Defines how to give the developer visibility into agent behaviour. Use before implementing any significant feature — communication protocols, BDI cycle, IActionBridge integrations. Claude cannot run Unity, so the developer is the one observing.
---

Before implementing anything significant, always provide the developer with tools to observe what is happening. Do not assume they can read the code and infer behaviour — make it visible.

After any implementation, always explain:
- What you added and why
- What the developer should see if it works (exact log lines, flashlight colour, console output)
- What the developer should see if it fails

## Available observability tools

Use whichever is simplest for the situation. Combine them when useful.

### Unity console
Acceptable for communication and planning events as long as each line is readable in isolation. Use a consistent prefix so logs can be filtered with the Unity console search bar.

```csharp
Debug.Log($"[FIPA] {sender} → {receiver} | {performative} | conv:{conversationId[..8]}");
Debug.Log($"[BDI]  {agentId} | GenerateOptions → {desires.Count} desires");
Debug.Log($"[BRIDGE] {agentId} | AssignTask {task.Type} contract:{task.ContractId[..8]}");
```

Prefixes to use: `[FIPA]`, `[BDI]`, `[BRIDGE]`, `[HTN]`, `[FSM]`

### Log file
Use for sequences of events that need to be read in order (e.g. a full negotiation). Write to `Assets/Logs/comms.log`. The developer can run `tail -f Assets/Logs/comms.log` in a terminal alongside Unity.

### Flashlight
Use to show internal state visually in the viewport without opening any window. Prefer this for state that persists over time (e.g. "has an active contract") rather than one-off events.

Suggested conventions — always document which ones you use:
- **Blue solid**: agent has an active contract from communication
- **Blue slow blink**: CFP open, waiting for proposals  
- **Green flash**: AcceptProposal received  
- **Red flash**: RejectProposal received  
- **White fast blink**: something is wrong / buffer pressure

Always tell the developer exactly which colour means what when you use the flashlight.