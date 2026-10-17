# 09 — Current Project Status (living document)

**Last updated:** 2026-10-17
**Source of truth:** Actual repository, verified commands, and developer confirmation.

## Goal

Learn C# and ASP.NET Core by building a small internal request approval system. Work incrementally and keep the MVP as one maintainable application.

## Completed milestones

### M0 — Bootstrap and first API

- [x] Created the ASP.NET Core Web API project.
- [x] Added `Request`, `RequestService`, and `RequestsController`.
- [x] Registered the service through dependency injection.
- [x] Verified `GET /api/requests` returns JSON.

### M1 — EF Core, SQL Server, and first test

- [x] Added EF Core SQL Server and `AppDbContext`.
- [x] Stored the local connection string in .NET User Secrets.
- [x] Generated and applied the `InitialCreate` migration.
- [x] Changed `RequestService.GetAllAsync()` to query SQL Server with `AsNoTracking()`.
- [x] Verified `GET /api/requests` returns rows stored in SQL Server.
- [x] Added an xUnit integration-test project.
- [x] Tests replace SQL Server with an isolated SQLite in-memory database.
- [x] Added a meaningful integration test for `GET /api/requests`.
- [x] Documented restore, migration, run, and test commands in README.
- [x] Initialized the Git repository, added `.gitignore`, and pushed the initial project to GitHub.

## Previous completed milestone

**M2 — Login, users, and RBAC. Complete.**

### M2 verified progress

- [x] Added ASP.NET Core Identity with `ApplicationUser` and Identity-backed `AppDbContext`.
- [x] Generated and applied the `AddIdentity` migration to local SQL Server.
- [x] Added the Employee, Manager, and Admin role constants.
- [x] Added an idempotent role seeder and verified the roles exist in SQL Server.
- [x] Configured JWT Bearer authentication using a signing key from User Secrets.
- [x] Added JWT authentication and authorization middleware in the correct order.
- [x] Added `TokenService` to create signed access tokens containing user and role claims.
- [x] Added an idempotent development-only seeder for Employee, Manager, and Admin accounts; no password is committed.
- [x] Verified all three demo users and their role assignments in local SQL Server.
- [x] Registered the named `ManagerOrAdmin` and `CanAccessRequests` authorization policies.
- [x] Added `POST /api/auth/login` and manually verified login against SQL Server.
- [x] Added authenticated `GET /api/auth/me`, returning only `id`, `email`, and `role` through a DTO.
- [x] Added required `Request.RequesterId` ownership with an EF Core foreign key to `AspNetUsers` and `DeleteBehavior.Restrict`.
- [x] Backfilled the three existing local requests to the demo Employee before making `RequesterId` required.
- [x] Protected `GET /api/requests` with JWT authentication.
- [x] Filtered request lists by verified JWT identity and role: Employee sees owned requests; Manager/Admin see all.
- [x] Added integration tests for valid login, invalid password, `/me` without a token, and `/me` with a valid token.
- [x] Added request authorization tests for 401, Employee ownership isolation, Manager access, and Admin access.
- [x] Added login and `/me` role coverage for Employee, Manager, and Admin.
- [x] Applied `CanAccessRequests` to request endpoints, requiring Employee, Manager, or Admin.
- [x] Verified a valid JWT without a recognized application role receives `403 Forbidden`.
- [x] Developer confirmed the full integration suite passes: 11/11 tests on 2026-10-16.

### M2 completion note

- `ManagerOrAdmin` is reserved for approval workflow endpoints introduced in M4; M2 does not add a placeholder endpoint solely to exercise this policy.
- M2 acceptance criteria are complete: all three demo roles can authenticate, request visibility follows ownership and role, and 401/403 behavior is covered by integration tests.

## Completed milestone

**M3 — Create/detail and validation. Complete.**

### M3 verified progress

- [x] Added required `Request.Category` with the default value `Other`.
- [x] Centralized the three fixed MVP category values in `RequestCategories`; no category table is needed yet.
- [x] Generated and applied the `AddRequestCategory` migration; existing requests were assigned `Other`.
- [x] Verified the API project builds after adding the property: 0 errors on 2026-10-16.
- [x] Added `CreateRequestDto` validation for title, description, and the three supported categories.
- [x] Added `RequestResponseDto` so request APIs do not expose the EF navigation object.
- [x] Added `RequestService.CreateAsync()` to persist a new Pending request owned by the authenticated user and return a response DTO.
- [x] Added authenticated `POST /api/requests`; requester ownership comes only from the JWT and successful creation returns `201 Created`.
- [x] Renamed the shared request policy to `CanAccessRequests` so its name covers both viewing and creating requests.
- [x] Added an integration test proving valid creation returns 201, assigns JWT ownership, and persists the row.
- [x] Cleared platform-specific logging providers in the test host so integration tests do not require Windows Event Log access.
- [x] Added validation integration coverage for invalid title, description, and category; all return 400 without persisting data.
- [x] Added ownership-aware `RequestService.GetByIdAsync()` with DTO projection; inaccessible requests return no result.
- [x] Added `GET /api/requests/{id}`, returning 200 for a visible request and 404 when missing or outside an Employee's ownership scope.
- [x] Added request-detail integration coverage for owner access, Employee isolation, Manager/Admin access, and a missing ID.
- [x] Changed request-list queries to return `RequestResponseDto` projections instead of EF entities.

### M3 completion note

- Request creation and detail retrieval persist correctly and enforce validation, authentication, roles, and ownership.
- Request history is intentionally deferred to the complete audit implementation in M4, as allowed by the milestone plan.
- The full integration suite passes 20/20 tests.

## Latest completed milestone

**M4 — Approval workflow and audit. Complete.**

### M4 verified progress

- [x] Added `RequestStatuses` constants for Pending, Approved, Rejected, Completed, and Cancelled.
- [x] Replaced raw request-status strings in application code and tests; no migration was required because persisted values did not change.
- [x] Added a database-independent `RequestStateMachine` covering the four allowed transitions.
- [x] Added unit tests for valid and invalid transitions: 9/9 state-machine cases pass.
- [x] Full suite remains green: 29/29 tests on 2026-10-17.
- [x] Added `RequestStatusHistory` with request/actor foreign keys, UTC timestamp, optional note, and restrictive delete behavior.
- [x] Added the `Request.StatusHistory` navigation and `AppDbContext.RequestStatusHistories` set.
- [x] Generated, reviewed, and applied the `AddRequestStatusHistory` migration to local SQL Server.
- [x] Request creation now writes the initial `null -> Pending` audit entry in the same `SaveChangesAsync()` transaction.
- [x] Creation tests verify both request and history persistence; invalid inputs persist neither.
- [x] Added `RequestStatusHistoryResponseDto` and ownership-aware, chronologically ordered history queries.
- [x] Added `GET /api/requests/{id}/history`, returning 200 for visible requests and 404 when hidden or missing.
- [x] Added integration coverage for history ownership, Manager/Admin visibility, missing IDs, and chronological ordering.
- [x] Added service-layer `ApproveAsync()` with state validation, self-approval prevention, atomic history persistence, and explicit transition outcomes.
- [x] Added service tests for successful approval, self-approval denial, repeat-approval conflict, missing requests, and audit persistence.
- [x] Added `POST /api/requests/{id}/approve` with `ManagerOrAdmin` authorization and 200/403/404/409 outcome mapping.
- [x] Added approval endpoint integration coverage for Employee denial, Manager/Admin success, self-approval denial, missing requests, repeat-approval conflict, and audit persistence.
- [x] Added `RejectRequestDto` with a required rejection reason of 5–500 characters.
- [x] Added service-layer `RejectAsync()` with state validation, self-rejection prevention, rejection reason audit persistence, and explicit transition outcomes.
- [x] Added service tests for successful rejection, self-rejection denial, invalid-state conflict, missing requests, and rejection audit persistence.
- [x] Added `POST /api/requests/{id}/reject` with DTO validation, `ManagerOrAdmin` authorization, and 200/403/404/409 outcome mapping.
- [x] Added rejection endpoint integration coverage for reason validation, Employee denial, Manager/Admin success, self-rejection denial, missing requests, invalid-state conflict, and audit persistence.
- [x] Added service-layer `CompleteAsync()` for the `Approved -> Completed` transition with state validation, audit persistence, and explicit transition outcomes.
- [x] Added service tests for successful completion, Pending-state conflict, missing requests, and the full Pending/Approved/Completed history sequence.
- [x] Added `POST /api/requests/{id}/complete` with `ManagerOrAdmin` authorization and 200/404/409 outcome mapping.
- [x] Added completion endpoint integration coverage for Employee denial, Manager/Admin success, missing requests, Pending-state conflict, and audit persistence.
- [x] Added service-layer `CancelAsync()` so only the request owner can transition `Pending -> Cancelled`, with state validation and audit persistence.
- [x] Added service tests for successful cancellation, non-owner denial, Approved-state conflict, missing requests, and audit persistence.
- [x] Added `POST /api/requests/{id}/cancel`, using JWT ownership checks and 200/403/404/409 outcome mapping.
- [x] Added cancellation endpoint integration coverage for owner success, Employee/Manager/Admin non-owner denial, missing requests, Approved-state conflict, and audit persistence.
- [x] Added `Request.RowVersion` and configured it as a SQL Server-generated optimistic concurrency token.
- [x] Generated and applied the `AddRequestRowVersion` migration to local SQL Server; SQLite remains a fast non-concurrency test provider.
- [x] Request transitions now catch `DbUpdateConcurrencyException` and return the existing Conflict outcome; EF Core rolls back the status and audit write together.
- [x] Added a separate SQL Server concurrency test project that uses an isolated temporary database.
- [x] Verified two managers approving the same `RowVersion` produce one success, one conflict, and exactly one approval audit entry.

### M4 completion note

- All Pending/Approved/Rejected/Completed/Cancelled transitions and their permissions are implemented.
- Audit history records actor, timestamp, previous status, target status, and rejection reason where applicable.
- Optimistic concurrency is verified against SQL Server rather than inferred from SQLite behavior.
- M4 acceptance criteria are complete.

## Known technical debt

- NuGet currently reports `NU1902`/`NU1903` vulnerability warnings for `Microsoft.OpenApi` 2.0.0 and `System.Security.Cryptography.Pkcs` 9.0.11. These warnings have not been resolved yet.

## Current milestone

**M4.5 — Backend query & demo-data hardening. Complete.**

- [x] Add a human-readable `FullName` to users and expose user name/email through request and history DTOs.
- [x] Add request search, status/category filters, allow-listed sorting, and bounded pagination.
- [x] Preserve Employee ownership filtering and Manager/Admin global visibility.
- [x] Add integration coverage for all list-query behavior and authorization.
- [x] Add query-driven composite indexes and inspect SQL Server execution plans.
- [x] Add an explicit local demo-data command for generating many fake records; do not seed requests automatically at API startup.
- [x] Defer caching, queues, background jobs, and external search until a measured requirement exists.

### M4.5 verified progress

- [x] Added required `ApplicationUser.FullName` with a 150-character limit.
- [x] Generated and applied the `AddUserFullName` migration to local SQL Server; developer confirmed the three demo users now have names.
- [x] Updated the idempotent demo-user seeder so both new and existing demo users receive a configured name or a safe role-based demo name.
- [x] Added `RequesterFullName` and `RequesterEmail` to request responses across list, create, detail, and transition results.
- [x] Added integration assertions for requester name/email; API build succeeds and the standard suite passes 75/75 tests.
- [x] Added validated request query parameters for search, status/category filters, and allow-listed sorting.
- [x] Connected query-string model binding to `GET /api/requests`; filtering remains constrained by Employee ownership before results are returned.
- [x] Added integration coverage for combined search/filter/sort behavior and invalid query values; the standard suite passes 82/82 tests.
- [x] Changed request lists to return `PagedResult<RequestResponseDto>` with page metadata calculated from filtered, authorization-scoped results.
- [x] Added pagination coverage for page contents, `page`, `pageSize`, `totalCount`, and `totalPages`; `RequestsApiTests` pass 27/27 and the standard suite passes 83/83.
- [x] Added request-list indexes for `(CreatedAt, Id)`, `(RequesterId, CreatedAt, Id)`, and `(Status, CreatedAt, Id)`; the redundant single-column requester index is replaced by the requester-leading composite index.
- [x] Limited `Request.Status` to `nvarchar(50)`, generated/reviewed `AddRequestListIndexes`, and applied it successfully to local SQL Server.
- [x] Verified the standard suite still passes 83/83 after the index migration.
- [x] Added an explicit `--seed-demo-data` command with a configurable 1–50,000 request count, 500-row batches, one transaction, matching audit histories, and duplicate prevention.
- [x] Seeded and manually verified 10,000 local SQL Server requests and 17,500 histories across all five statuses; a second invocation inserted nothing.
- [x] Added one automated seeder integration test covering status distribution, audit consistency, actor rules, rejection notes, and idempotency; the standard suite passes 84/84.
- [x] Inspected actual SQL Server plans using 10,000 requests: ownership and status-filter queries use their matching composite indexes; substring title/description search scans as expected for a leading-wildcard search.
- [x] Recorded the current measured limit: the 10,000-request local demo workload does not justify cache, queues, background jobs, or an external search engine. Reassess substring search only if measured latency or data volume becomes a real bottleneck.

## Last verification

- Date: 2026-10-17
- Build: `dotnet build tests/RequestManagementSystem.SqlServerTests/RequestManagementSystem.SqlServerTests.csproj` succeeds after the M4 concurrency implementation (0 errors; known package warnings remain).
- Database: developer confirmed Employee, Manager, and Admin demo users exist and each has the correct role in local SQL Server.
- Audit database: `AddRequestStatusHistory` was applied successfully to local SQL Server on 2026-10-17.
- Concurrency database: `AddRequestRowVersion` was applied successfully to local SQL Server on 2026-10-17.
- Manual auth: developer confirmed login returns an access token.
- Tests: `dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj` passes 75/75 tests after adding cancellation endpoint coverage.
- SQL Server concurrency test: `dotnet test tests/RequestManagementSystem.SqlServerTests/RequestManagementSystem.SqlServerTests.csproj --no-restore` passes 1/1 using an isolated temporary database.
- M4.5 request identity: `dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj --no-restore` succeeds and `dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj --no-restore` passes 75/75 on 2026-10-17.
- M4.5 list queries: filtered `RequestsApiTests` pass 26/26 and the full standard suite passes 82/82 on 2026-10-17.
- M4.5 pagination: filtered `RequestsApiTests` pass 27/27 and the full standard suite passes 83/83 on 2026-10-17.
- M4.5 indexes: developer confirmed `AddRequestListIndexes` was applied to local SQL Server; the full standard suite passes 83/83 afterward.
- M4.5 demo data: developer confirmed 10,000 requests and 17,500 histories in local SQL Server; automated seeder coverage passes and the full standard suite passes 84/84.
- M4.5 query plans: developer confirmed the expected execution-plan operators for Employee ownership, Manager status filtering, and substring search. The Employee checks reported about 50–52 logical reads with zero physical reads.

## Next single task

Review and commit the completed M4.5 backend work before starting the first M5 React task.
