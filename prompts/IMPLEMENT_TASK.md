# Implement Task Prompt

Use this with Codex:

```txt
Read docs/MASTER_PRD_FOR_AI.md, AGENTS.md, and [TASK_FILE].

Implement only the selected task.
Do not build unrelated features.
Follow the React frontend + .NET Core backend + PostgreSQL architecture.
Keep /theme as a sample-data playground.
Use shared components from src/app/src/components for both app and theme UI.
Do not use XYFlow unless the task is specifically about workflow builder.
Enforce permissions on the backend.
Add or update tests where practical.
Run the relevant build/test commands if available.

At the end, summarize:
1. What changed
2. Files changed
3. Tests/builds run
4. Risks
5. Follow-up tasks
```
