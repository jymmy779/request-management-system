# 05 — Milestones & Acceptance Criteria

> **Không code tất cả cùng lúc.** Đây là roadmap. Codex chỉ thực hiện **một task nhỏ trong milestone đang làm** sau khi đọc `09-PROJECT-STATUS.md`.
>
> Ước tính tổng thể: MVP có thể cần **3–6 tuần** hoặc lâu hơn khi bắt đầu học C#; không cam kết deadline cố định.

## M0 — Verify current scaffold & DI

**Mục tiêu:** `GET /api/requests` thực sự gọi qua `RequestService.GetAll()` và chạy ổn.

Tasks:
- [ ] Kiểm tra source hiện tại, không giả định những gì tồn tại.
- [ ] Chuẩn hóa tên file/class `RequestsController.cs` / `RequestsController` nếu bị sai.
- [ ] Kiểm tra `RequestService.GetAll()` đã tồn tại và trả `List<Request>` dữ liệu demo.
- [ ] Kiểm tra `builder.Services.AddScoped<RequestService>()` trong `Program.cs`.
- [ ] Dùng constructor injection trong controller, tránh trộn primary constructor với constructor truyền thống.
- [ ] Chạy `dotnet build`; test `GET /api/requests` trả JSON 200.
- [ ] Xác nhận với developer và cập nhật STATUS.

**DoD:** Build pass, GET đi đúng Controller -> Service và trả dữ liệu. **Không** thêm DB, JWT, Docker ở M0.

## M1 — EF Core + SQL Server + first tests

**Mục tiêu:** thay hardcoded list bằng data thật, hiểu EF Core/LINQ/migrations.

- [ ] Chọn/cấu hình SQL Server Express hoặc local container, kết nối bằng user secrets/env.
- [ ] Thêm EF Core SQL Server provider + CLI tool nếu cần.
- [ ] Tạo DbContext và migration; dữ liệu demo local có thể nhập thủ công, không thêm automatic development seeder.
- [ ] Chuyển `GetAll()` sang async truy vấn DB (giữ API contract).
- [ ] Tạo xUnit test project, thêm integration test cho GET + DB behavior.
- [ ] Build + tests pass, README thêm cách khởi tạo DB.

**DoD:** Tắt/bật lại API vẫn giữ dữ liệu; query lấy từ SQL Server; test dùng dữ liệu cách ly và chạy trên môi trường local được tài liệu hóa.

## M2 — Login / users / RBAC

- [ ] ASP.NET Core Identity, roles Employee/Manager/Admin.
- [ ] JWT Bearer config từ secrets, seed **demo** accounts (không commit password thật).
- [ ] Login + me endpoints, `[Authorize]` và role policies.
- [ ] Liên kết request demo hiện có với user seed; lọc GET list theo ownership (Employee xem của mình, Manager/Admin xem tất cả).
- [ ] Tests: 401/403, owned vs non-owned requests, role coverage.

**DoD:** 3 role demo đăng nhập được; không xem dữ liệu trái quyền; tests pass. Nếu M1 seed requests chưa có RequesterId, bổ sung migration/backfill an toàn cho môi trường dev.

## M3 — Create/detail & validation

- [ ] DTO `CreateRequestDto`, `RequestResponseDto`; validation (Title/Description/Category).
- [ ] `POST /api/requests` trả 201, lấy requester từ JWT; `GET /api/requests/{id}` trả 200 hoặc 404 theo quyền.
- [ ] Service async, EF Core SaveChanges; test valid/invalid input, not found, owner isolation.
- [ ] Ghi history khi tạo, hoặc triển khai history đầy đủ ở M4.

**DoD:** Endpoint chạy, data persist, validation 400, 401/403/404 đúng quyền và tests pass.

## M4 — Approval workflow + audit

- [ ] State machine Pending/Approved/Rejected/Completed/Cancelled.
- [ ] Approve/reject/complete/cancel endpoints; reject phải có reason.
- [ ] Không self-approve; history có actor/timestamp và transaction.
- [ ] Chống cập nhật đồng thời, trả 409 khi conflict.
- [ ] Unit + integration tests cho state transitions, permissions, concurrency.

**DoD:** Demo từ lúc tạo đến Completed; các transition sai bị chặn; tests pass.

## M4.5 — Backend query & demo-data hardening (COMPLETE)

**Mục tiêu:** làm API danh sách đủ dùng cho frontend và kiểm tra hành vi truy vấn với lượng dữ liệu demo lớn trên SQL Server local.

- [x] Bổ sung `FullName` cho user; response request/history hiển thị tên và email thay vì chỉ có ID khó đọc. Giữ `Role` để biểu diễn quyền hệ thống; chưa thêm `JobTitle` vì chưa có nghiệp vụ cần chức danh công việc.
- [x] Thêm tìm kiếm theo title/description, lọc theo status/category và sắp xếp bằng danh sách trường cho phép.
- [x] Thêm phân trang có giới hạn `pageSize`, tổng số bản ghi và tổng số trang; mặc định sắp xếp mới nhất trước.
- [x] Giữ ownership filter trước khi trả dữ liệu: Employee chỉ thấy request của mình; Manager/Admin thấy toàn bộ.
- [x] Thêm integration tests cho tìm kiếm, lọc, sắp xếp, phân trang và phân quyền.
- [x] Thêm composite index phù hợp với các query thực tế; tạo migration và xem execution plan trên SQL Server trước/sau khi thêm index.
- [x] Thêm lệnh seed demo chạy chủ động cho SQL Server local, tạo dữ liệu giả số lượng lớn theo batch và không tự chạy mỗi lần khởi động API.
- [x] Ghi lại giới hạn đã kiểm chứng; chưa thêm cache, queue, background job hoặc search engine nếu chưa có bottleneck chứng minh nhu cầu.

**DoD:** List API trả dữ liệu dễ đọc và có search/filter/sort/pagination; quyền truy cập vẫn đúng; tests pass; dữ liệu demo lớn có thể tạo lại an toàn; index và query plan được kiểm tra trên SQL Server.

## M5 — React UI (small) (CURRENT)

- [x] Vite + React + TypeScript; login/logout bằng HttpOnly cookie và protected routes.
- [x] App layout có sidebar; request list/detail/create/edit và history.
- [x] Manager/Admin có approve/reject/complete; owner có cancel theo state machine.
- [x] Loading/error/empty states, UTC -> local time và toast phản hồi thao tác.
- [x] Hiển thị requester, department, priority, quantity, estimated cost và updated time.
- [ ] Bổ sung priority filter end to end; thêm index theo query thực tế và kiểm tra lại execution plan.
- [ ] Thêm API danh sách phòng ban để form/filter không hard-code dữ liệu.
- [ ] Thêm My Approvals đơn giản trên query contract hiện có hoặc mở rộng tối thiểu khi cần.
- [ ] Thêm dashboard thống kê tối giản theo vai trò; không triển khai report/analytics chuyên sâu trong M5.
- [ ] API e2e manual smoke checklist cho Employee/Manager/Admin; chạy lại backend tests sau thay đổi.

Thiết kế AdminHub chỉ là tham khảo hình ảnh và bố cục. M5 không bao gồm workflow duyệt nhiều cấp, vendor, policy, module request riêng theo từng phòng ban, attachment, comments, save draft hoặc reporting chuyên sâu.

**DoD:** Demo full flow không cần Postman; backend tests vẫn pass.

## M6 — Portfolio quality

- [ ] Docker Compose cho API + DB (nếu phù hợp cấu hình máy).
- [ ] Test automation GitHub Actions; README setup/run/test.
- [ ] No secrets in Git; demo data; API docs OpenAPI; screenshots.
- [ ] Check lint/build/test and reproduce setup from clean instructions.

**DoD:** Clone/build/test/demo có thể tái lập và ghi rõ prerequisites.

## M7 — Microservices OPTIONAL, only after M6

- [ ] Notification Worker thành process .NET độc lập, database riêng.
- [ ] RabbitMQ event `RequestApproved`, consumer idempotency, retry.
- [ ] Integration tests qua broker; document failure scenarios.
- [ ] Không viết "microservices" trong CV nếu chỉ có hosted worker cùng API.

**DoD:** API và Notification Worker khởi động/triển khai riêng, không chia sẻ bảng DB, có demo event processing.

## Completion checklist cho **mỗi milestone**

- [ ] Chỉ scope đã duyệt được triển khai.
- [ ] `dotnet build` và tests liên quan đã chạy (ghi kết quả thật).
- [ ] API được smoke-test khi có endpoint mới.
- [ ] STATUS cập nhật: done/blocked/next; DECISIONS nếu thay đổi kiến trúc.
- [ ] Developer hiểu và giải thích được đoạn C# chính vừa thêm.
