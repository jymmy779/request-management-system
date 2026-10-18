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
| 2026-10-17 | Separate SQL Server test project for concurrency | SQL Server `rowversion` semantics cannot be verified by SQLite; the dedicated suite creates and removes a uniquely named temporary database without touching the development database | Accepted |
| 2026-10-17 | Add M4.5 backend query and demo-data hardening before React | The frontend needs readable user data and scalable list behavior; local high-volume demo data will also provide a concrete basis for learning indexes and query plans. Cache, queue, background jobs, and external search remain deferred until justified by measured behavior | Accepted |
| 2026-10-17 | Browser authentication uses a server-issued HttpOnly JWT cookie | Prevent frontend JavaScript from reading or persisting the access token; CORS uses an explicit origin plus credentials, and logout removes the cookie. Bearer-header validation remains supported for non-browser tests and clients | Accepted |
| 2026-10-17 | Request owners may edit only Pending request content | Editing is useful for the MVP, but ownership and state restrictions prevent managers from rewriting employee requests or changing content after workflow decisions; editable fields are title, description, and category | Accepted |
| 2026-10-17 | Model departments as a separate entity linked optionally from users | Department data needs stable codes, future filtering/reporting, and eventual user administration. The initial nullable link preserves existing users and does not yet alter Manager/Admin visibility rules | Accepted |
| 2026-10-18 | Defer `RequiredByDate` | Priority already communicates urgency, while the current product has no SLA, scheduling, overdue alerts, or deadline-driven workflow to give a required-by date reliable business meaning | Accepted |
| 2026-10-18 | Use the AdminHub screen as a visual reference, not a 1:1 product specification | The reference includes multi-level approval, vendors, policies, attachments, comments, drafts, reports, and separate departmental modules that would expand the small portfolio MVP into an ERP-like product. M5 keeps the existing single-step Manager/Admin workflow and implements only the UI needed to demonstrate the verified request lifecycle | Accepted |
| 2026-10-18 | Model My Approvals as the current user's actionable queue, not an assignment inbox | The MVP has no approver-assignment entity. Manager/Admin therefore see Pending requests created by other users; adding wording or behavior that implies a specific assignee would misrepresent the data model | Accepted |
| 2026-10-18 | Keep account creation Admin-only and delegate password storage to ASP.NET Core Identity | The MVP has no public signup. Admin supplies identity fields, one application role, and an optional department; Identity enforces its password policy and stores only a password hash | Accepted |
| 2026-10-18 | Separate authenticated password changes from forgotten-password recovery | Change password requires the current password and signs out the current browser. Forgot password must use a time-limited Identity token delivered out of band; the API must not expose a reset token directly | Accepted |
| 2026-10-18 | Use Mailpit only for local email capture behind an SMTP email abstraction | Mailpit is free and suitable for development but not production delivery. Deployment can replace configuration or the sender implementation without changing forgot-password business logic; SMTP credentials remain outside source control | Accepted |

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
