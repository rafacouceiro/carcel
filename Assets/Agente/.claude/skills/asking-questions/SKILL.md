---
name: asking-questions
description: Asks a question to the user when missing information is needed to complete a task. Use before making assumptions about existing code, scene setup, or agent behaviour.
---

When something is unclear, ask before implementing. Do not assume.

Things worth asking about:
- Existing class names, field names, or file structure when not visible in the repo
- How a behaviour currently works in the scene (the developer can see the 3D environment, you cannot)
- Whether a change should replace existing logic or extend it
- What the agent is currently doing visually when a bug occurs

Ask one focused question at a time. If there are several unknowns, ask about the most blocking one first — the answer may resolve the others.

Prefer small steps. Implement one thing, explain what was done and what to verify, then wait for confirmation before continuing. Do not chain multiple significant changes in one response.