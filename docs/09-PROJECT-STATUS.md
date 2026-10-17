# 09 — Current Project Status (living document)

**Last updated:** 2026-10-18
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

**M5 — React UI (small). In progress.**

- [x] Created `frontend/` with Vite, React, TypeScript, and ESLint.
- [x] Removed the Vite starter screen and unused starter assets; added a minimal responsive application shell.
- [x] Verified the frontend with `npm run build --prefix frontend` and `npm run lint --prefix frontend`.
- [x] Configured an allow-listed ASP.NET Core CORS policy for `http://localhost:5173`; a real preflight request returns the expected allow-origin, method, and header values.
- [x] Added environment-based frontend API configuration and a typed `login()` function matching the backend contract.
- [x] Added a controlled React login form with pending, invalid-credential, and success states; developer verified a successful login against the local API.
- [x] Migrated browser authentication from a token JSON response to a server-issued HttpOnly JWT cookie; frontend requests include credentials and JavaScript never receives the token.
- [x] Added cookie-based `/api/auth/me` authentication and `/api/auth/logout`; the full backend suite passes 85/85 tests, including cookie attributes and logout behavior.
- [x] Added frontend session restoration through `/api/auth/me`, an authenticated user shell, and a logout action; frontend build and lint pass.
- [x] Centralized the frontend `/api` base path and added a typed request-list client supporting the backend search, filter, sort, and pagination query contract.
- [x] Split the login form and authenticated shell into focused React components instead of keeping the whole UI in `App.tsx`.
- [x] Added a route-level `LoginPage`; `LoginForm` now contains only form state and controls.
- [x] Added React Router with `/login` and protected `/requests` routes; route declarations are centralized in `routes/AppRoutes.tsx`, while root and unknown paths redirect according to the restored server session.
- [x] Connected the frontend to the backend API and implemented cookie-based login/logout.
- [x] Replaced the authenticated placeholder with the first request-list page, including loading, retryable error, empty, and populated states; the first 20 requests render with status badges and local timestamps.
- [x] Added a shared authenticated `AppLayout` with responsive sidebar navigation, account context, and logout; protected pages render inside it through a nested route outlet.
- [x] Added server-side Previous/Next request pagination using the API's current-page, total-count, and total-pages metadata.
- [x] Added a dedicated request filter component for title/description search, status/category filters, and allow-listed sorting; applying or clearing criteria resets the server-side list to page 1.
- [x] Added a protected `/requests/:requestId` detail page backed by the ownership-aware request-detail API; list titles link to it and loading, retryable error, inaccessible/missing, and invalid-ID states are handled.
- [x] Added a protected `/requests/new` page, reusable validated form, typed create-request API client, and sidebar navigation; successful creation redirects to the new request detail page. Frontend build and lint pass; browser/API creation remains to be manually verified.
- [x] Added `PUT /api/requests/{id}` for owner-only Pending request editing, with DTO validation, optimistic-concurrency conflict handling, and seven service/API tests.
- [x] Redesigned frontend to match AdminHub approval workflow design: dark navy sidebar, workflow stepper, 2-column detail layout, interactive approval/rejection decision cards, and audit history timeline.
- [x] Connected frontend workflow transitions (`approve`, `reject`, `complete`, `cancel`) and history retrieval (`GET /api/requests/{id}/history`).
- [x] Enhanced `/requests/new` and `/requests/:id/edit` with AdminHub 2-column layout: interactive category cards, live character counters, approval workflow preview stepper, editing policies, and requester profile snippet.
- [x] Upgraded `RequestFilters` on `/requests` with quick status segmented tabs, search box with clear button, instant category/sort updates, and active filter tags.
- [x] Overhauled the Login page (`/login`) to the AdminHub aesthetic: deep navy ambient backdrop, elevated white card with glowing accents, AdminHub brand header, icon-adorned input fields, password visibility toggle, loading states, and enterprise RBAC footer (without demo shortcuts as requested).
- [x] Verified frontend with `npm run build` and `npm run lint`: 0 errors.
- [x] Added the backend Department foundation: a normalized `Department` entity, unique department code, and an optional `ApplicationUser.DepartmentId` foreign key using `SET NULL`; generated, reviewed, and applied the `AddDepartments` migration.
- [x] Added an idempotent development department seeder for IT, Human Resources, Finance, Operations, and Procurement; demo Employee and Manager users belong to IT while the demo Admin belongs to Operations, without changing request authorization rules.
- [x] Exposed nullable department names through `/api/auth/me` and all request responses, and replaced the hard-coded frontend department labels with API data plus an unassigned fallback.
- [x] Added request priority (`Low`, `Medium`, `High`) end to end across EF Core, create/update validation, API responses, tests, and React create/edit/list/detail views; existing SQL Server requests were backfilled to `Medium` by `AddRequestPriority`.
- [x] Added request quantity end to end with a 1–1,000 range across EF Core, create/update validation, API responses, tests, and React create/edit/list/detail views; existing SQL Server requests were backfilled to `1` by `AddRequestQuantity`.
- [x] Added optional VND estimated cost end to end using `decimal(18,2)`, positive-value validation, API responses, tests, and React create/edit/list/detail views; existing requests remain `NULL` rather than receiving fabricated costs.
- [x] Added nullable `UpdatedAt` end to end; content edits and successful workflow transitions maintain it while newly created and untouched requests keep it `NULL`. The detail UI displays the latest update in local time.
- [x] Enhanced future demo-request generation with deterministic Low/Medium/High priorities, quantities from 1–5, category-based optional estimated costs, and `UpdatedAt` values consistent with the final audit entry.
- [x] Added the Development-only `--backfill-demo-data` command, which updates only `[DEMO-LOAD]` requests in 500-row batches and skips rows already carrying the expected values.
- [x] Added a reusable global toast provider for create, update, approve, reject, complete, and cancel feedback; up to five concurrent notifications stack without overwriting each other, survive route navigation, have independent auto-dismiss timers, visible SVG status icons, fade-out transitions, manual dismissal, and appropriate live-region roles.
- [x] Scoped the AdminHub design reference for M5: preserve the current single-step Manager/Admin workflow and treat multi-level approval, vendors, policies, attachments, comments, save drafts, and advanced reports as post-MVP backlog rather than implied requirements.

## Completed milestone

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
- M5 request update backend: filtered update tests pass 7/7 and the full standard suite passes 92/92 in Release on 2026-10-17. Debug output remained locked only because the development API was running.
- M5 department foundation: the API Release build succeeds and the standard suite passes 92/92 after generating `AddDepartments`; the migration still needs to be applied to local SQL Server.
- M5 department seeding: `AddDepartments` is applied to local SQL Server; the idempotency test passes and the full standard suite passes 93/93. Restarting the Development API runs the seeder and updates existing demo users.
- M5 department responses: backend tests pass 93/93; frontend production build and ESLint both pass on 2026-10-18.
- M5 request priority: `AddRequestPriority` was applied successfully; backend tests pass 94/94 and frontend production build/ESLint both pass on 2026-10-18.
- M5 request quantity: `AddRequestQuantity` was applied successfully; backend tests pass 95/95 and frontend production build/ESLint both pass on 2026-10-18.
- M5 estimated cost: `AddRequestEstimatedCost` was applied successfully; backend tests pass 96/96 and frontend production build/ESLint both pass on 2026-10-18.
- M5 latest update timestamp: `AddRequestUpdatedAt` was applied successfully; backend tests pass 96/96 and frontend production build/ESLint both pass on 2026-10-18.
- M5 demo request fields: the focused seeder test passes 1/1 and the full standard suite passes 96/96 on 2026-10-18.
- M5 demo backfill: the local command updated 10,000 `[DEMO-LOAD]` requests; a second run reported that all demo requests were already up to date. The focused test proves non-demo data remains unchanged, and the full standard suite passes 96/96.
- M5 toast feedback: frontend production build and ESLint pass on 2026-10-18.

## Next single task

Add the request priority filter end to end (validated API query, EF Core filtering, integration coverage, frontend control), then add only the query-driven category/priority indexes justified by that list query.
