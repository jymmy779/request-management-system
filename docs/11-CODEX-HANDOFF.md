# 11 — Codex Session Handoff Protocol

## Why this file exists

ChatGPT/Codex conversations can get long or be restarted. Project context must live in the **repository**, not in chat history.

## Before each new Codex session

1. Open repository root in VS Code.
2. Read `AGENTS.md`, `docs/09-PROJECT-STATUS.md`, `docs/05-MILESTONES.md`, then relevant feature docs.
3. Inspect actual source tree, `git status`, build errors and existing tests.
4. Compare docs against code. If docs disagree, report mismatch; **do not guess**.
5. Work on only **the next agreed task**; make it small and reviewable.

## After each coding task

Codex should summarize:

```text
Task:
Files changed:
Behavior implemented:
What I learned (new .NET/C# concepts):
Build command + actual result:
Test command + actual result:
Manual test steps:
Remaining blockers:
Next ONE task:
```

Then update `09-PROJECT-STATUS.md`. For a durable product/architecture change also update `10-DECISIONS.md` and any affected PRD/API/model docs. Don't mark “Done” based on code generation alone.

## After each milestone

- Verify DoD against `05-MILESTONES.md`.
- Test all non-UI behavior that was added or changed; run regression tests.
- Get developer to manual-test UI when appropriate.
- Save concise handoff notes in this file under `## Latest handoff` or in STATUS.

## Latest handoff

- **Date:** 2026-10-14.
- **Completed stage:** M0 — Controller -> Service -> API flow verified.
- **Verified source:** `RequestService.GetAll()` returns demo `List<Request>`; `Program.cs` registers the scoped service; `RequestsController` uses conventional constructor DI and returns `Ok(_requestService.GetAll())`.
- **Build:** `dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj` succeeded with 0 errors. It reported `NU1903` for transitive dependency `Microsoft.OpenApi 2.0.0`; review this warning before adding M1 packages.
- **Endpoint:** developer confirms `GET /api/requests` is running and returns JSON successfully.
- **Naming note:** filenames remain `RequestServices.cs` and `RequestsControllers.cs`, while their public class names are correctly singular. No rename was made because this is not a runtime/build error.
- **Current stage:** M1 — EF Core + SQL Server + first tests; no M1 implementation has started.
- **Next ONE task:** inspect local database options, choose SQL Server Express or SQL Server in Docker, and document the selected prerequisite. Do not yet install EF Core packages or create database code.
- **Avoid:** combining database selection with `DbContext`, migrations, auth, frontend, Docker orchestration, or microservices in one task.

## Guard against context drift

Never infer that an endpoint/service/test exists just because `04-API-CONTRACT.md` describes it. PRD/API documents describe **desired behavior**. STATUS + actual repo establish **implemented behavior**. If code was changed outside Codex, re-scan before writing.
