---
name: git-workflow
description: Guides commit discipline during development. Use when making any code change, creating files, or before and after any integration step.
---

Commit often, one purpose per commit. Each commit must leave the project in a state that compiles and doesn't break existing behaviour.

## When to commit
- Before modifying any existing file (`WorldState`, `HTNPlanner`, `Brain`)
- After a new file compiles cleanly
- After any verified integration step
- Before any refactor, however small

## Commit message format

```
<type>(<scope>): <what it does>

Antes: <previous behaviour>     ← include when changing existing code
Después: <new behaviour>
Riesgo: <what could break>      ← include when touching shared systems
```

Types: `add`, `wire`, `fix`, `guard`, `refactor`, `test`

Good examples:
```
wire(Brain): inherits FIPAAgent, HTN behaviour unchanged
Antes: Brain : MonoBehaviour
Después: Brain : FIPAAgent
Riesgo: FIPAAgent.Awake must call base.Awake or MessageBus registration fails

guard(Filter): skip propose if QueryCapability returns false
```

If the message needs "and also" — split into two commits.

## When something breaks
```bash
git diff HEAD~1                                          # see exactly what changed
git checkout HEAD~1 -- Assets/Scripts/FileName.cs       # restore one file without reverting everything
```

If the project is already broken when you start working, commit that state first with `checkpoint: broken state before investigating X` — then you have a safe reference point.