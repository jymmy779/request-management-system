# 10 — Architecture Decision Log (ADR-lite)

Update only if developer agrees or code proves a decision was changed. Never silently change direction.

| Date | Decision | Reason | Status |
|---|---|---|---|
| 2026-10-14 | Product: Request Management System | Internal enterprise workflow suitable for .NET backend skills | Accepted |
| 2026-10-14 | Main backend: C# + ASP.NET Core Web API | Broaden CV beyond JavaScript; learn .NET | Accepted |
| 2026-10-14 | IDE: VS Code + C# Dev Kit + Codex | User is familiar with VS Code/Codex | Accepted |
| 2026-10-14 | Controller name: `RequestsController` | Standard REST naming; route `/api/requests` | Accepted |
| 2026-10-14 | Prefer conventional constructor DI | Beginner-friendly; avoid primary constructor mixing | Accepted |
| 2026-10-14 | API-first, one task at a time | Learn by implementing small verifiable features | Accepted |
| 2026-10-14 | Monolith first, microservices optional after MVP | Avoid overengineering while new to .NET | Accepted |
| 2026-10-14 | SQL Server + EF Core target | Practical .NET database skills | Proposed until DB milestone |
| 2026-10-14 | React + TypeScript frontend | Reuse existing FE skills; focus .NET backend | Proposed until UI milestone |
| 2026-10-14 | Identity + JWT for auth | Safer than custom password handling | Proposed until auth milestone |
| 2026-10-14 | No automatic development request seeder in M1 | Developer prefers to manage local demo rows manually and avoid persistent sample data; integration tests will create isolated test data | Accepted |
| 2026-10-14 | SQLite in-memory for M1 GET integration tests | Fast, isolated test data without touching the local SQL Server development database; SQL Server remains the application database and is still required for provider-specific/concurrency tests later | Accepted |

## Decision template

```text
Date:
Decision:
Alternatives considered:
Reason:
Trade-offs:
Affected docs/code:
Accepted by developer?:
```

Note: "Accepted" here refers to decisions established in the conversation; it **does not mean the related code has been implemented**.
