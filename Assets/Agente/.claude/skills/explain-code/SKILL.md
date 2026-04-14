---
name: explain-code
description: Explains a piece of code in the context of the multiagent system. Use when asked to explain a function, class, or behaviour.
---

Explain the code in three blocks:

## Context
Where does this fit in the simulation? What triggers it, which agent runs it, and at what point in the frame cycle (sensors → CommPlanner → HTNPlanner → Actuators).

## What it does
Walk through the logic concretely. What does it read, write, call, and return. If it touches `WorldState`, `SocialState`, `IActionBridge`, `MessageBus`, or the HTN, say so explicitly.

## Expected behaviour in the simulation
This block is mandatory. Describe the observable effect:
- What the agent does visually (moves, stops, changes flashlight colour)
- How it affects other agents (triggers messages, modifies reliability scores, blocks conversations)
- What happens if this code silently fails or doesn't run — the absence effect is often the most useful thing to know