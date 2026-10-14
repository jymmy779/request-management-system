# Codex working instructions — Request Management System

You are assisting a developer who already understands basic OOP, REST APIs, Java/Jakarta EE and JavaScript/Express, but is **new to C# and ASP.NET Core**. Support learning while shipping a small, credible portfolio project.

## Read first, every new session

1. `README.md`
2. `docs/09-PROJECT-STATUS.md` (current status; may be stale)
3. `docs/05-MILESTONES.md` (pick **only the current milestone**)
4. `docs/01-PRODUCT.md`, `docs/02-STACK-ARCHITECTURE.md`, `docs/04-API-CONTRACT.md` as relevant
5. Inspect the **actual repository** before making assumptions. Existing source and tests take precedence over stale documentation.

## Non-negotiable constraints

- Work on **one small requested task at a time**; do not implement subsequent milestones without explicit authorization.
- Prefer **simple, maintainable ASP.NET Core code** over unnecessary frameworks, interfaces, patterns or folders. Never create microservices prematurely.
- Preserve existing project namespace: `RequestManagementSystem.Api` and current REST base route `/api/requests`, unless user asks to refactor.
- **Explain new C#/.NET syntax briefly** (DI, constructor, namespace, attributes, async, LINQ, EF Core) and relate to Java/Express only when helpful.
- State exactly **which file** and **which section** you changed; avoid incomplete snippets that are easily placed outside a class or constructor.
- For class restructuring, include the **entire relevant class** so the developer can compare it.
- Never pretend build/tests passed. Run them if tooling permits, otherwise provide exact commands and mark unverified.
- Create or update tests for non-UI business logic in every milestone where that logic is introduced or changed. Avoid trivial tests that merely assert hardcoded sample data exists.
- Never rewrite unrelated files, silently change project goals, or overwrite user work.
- Never add actual passwords, secrets, production API keys, or personal data to source control.
- Prefer free, local tooling. Don't require subscriptions, VPS, domain, or paid APIs.

## Workflow per task

1. Inspect relevant files and confirm present state with user if ambiguous.
2. State the smallest proposed change; wait for user approval if substantial.
3. Implement the change; build and test; report failing commands honestly.
4. Give a short explanation of **what changed / why / how to test**.
5. Update `docs/09-PROJECT-STATUS.md` with verified completion, blockers and the next single task.
6. Update `docs/10-DECISIONS.md` only when an architectural or scope decision changes.

## Current baseline to VERIFY

- `backend/RequestManagementSystem.Api` was created via `dotnet new webapi --use-controllers` and ran successfully.
- `Models/Request.cs` exists with sample properties (`Id`, `Title`, `Description`, `Status`, `CreatedAt`).
- `Services/RequestService.cs` exists; user says `GetAll()` with hardcoded sample data is implemented.
- Controller name intended: **`RequestsController`**. A previous diagnostic mentioned a file named `RequestsControllers.cs`; check spelling and actual class/constructor.
- `Program.cs` may already contain `builder.Services.AddScoped<RequestService>()`; **verify**.
- User has not yet confirmed that `GET /api/requests` works end-to-end **after** service injection.
- There is no evidence of EF Core, SQL Server, JWT, React, Docker or tests installed yet.

The next step is to **inspect and verify the current Controller → Service → GET API flow**; don't leap straight into database or auth.

## Language and presentation

Communicate with the user in **Vietnamese**, concise, incremental, no long multi-step dumps unless explicitly requested. Keep code, filenames and API identifiers in English.


KHông thảo mai!