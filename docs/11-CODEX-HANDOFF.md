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

- **Date:** 2026-10-17.
- **Completed stage:** M4 — approval workflow, audit history, authorization, and SQL Server optimistic concurrency are complete.
- **Verified behavior:** the standard test suite passes 75/75; the isolated SQL Server concurrency suite passes 1/1. Local SQL Server has all migrations through `AddRequestRowVersion`.
- **Current stage:** M4.5 — backend query and demo-data hardening, added before the React milestone by developer agreement.
- **Planned scope:** readable user identity (`FullName` plus email), request search/filter/sort/pagination, authorization coverage, query-driven indexes, execution-plan inspection, and an explicit high-volume local demo-data command.
- **Deferred:** React remains M5. Cache, queue, background jobs, and external search are not added without a measured need.
- **Next ONE task:** add `FullName` to `ApplicationUser` only; build before creating its migration.
- **Avoid:** implementing query features, indexes, data generation, or frontend in the same task.

## Guard against context drift

Never infer that an endpoint/service/test exists just because `04-API-CONTRACT.md` describes it. PRD/API documents describe **desired behavior**. STATUS + actual repo establish **implemented behavior**. If code was changed outside Codex, re-scan before writing.
