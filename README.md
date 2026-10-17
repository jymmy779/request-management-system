# Request Management System

**Internal Request & Approval Management System** — ứng dụng web quản lý yêu cầu và quy trình phê duyệt nội bộ, làm theo hướng **backend-first** để học C# / ASP.NET Core và xây dựng portfolio ứng tuyển .NET Intern/Fresher.

> Tình trạng: **đang phát triển / chưa hoàn thành MVP**. Đọc [`docs/09-PROJECT-STATUS.md`](docs/09-PROJECT-STATUS.md) trước khi làm tiếp. Nội dung tài liệu là **đặc tả mục tiêu**, không phải tuyên bố các tính năng đã chạy.

## Vấn đề được giải quyết

Nhân viên gửi yêu cầu nội bộ (ví dụ: xin cấp màn hình, phần mềm, thiết bị). Quản lý phê duyệt hoặc từ chối; người phụ trách hoàn tất; hệ thống lưu lịch sử để các bên theo dõi trách nhiệm và tiến độ.

## Công nghệ (mục tiêu)

- Backend: **C# / .NET 10 LTS / ASP.NET Core Web API** (Controllers)
- Data: **EF Core + SQL Server**; migration và dữ liệu demo local được quản lý thủ công
- Auth: **ASP.NET Core Identity + JWT Bearer** (làm ở milestone sau)
- Frontend: **React + TypeScript + Vite** (làm sau khi API ổn)
- Testing: **xUnit**, unit + integration tests
- Dev: **VS Code + Codex**, Docker Compose (giai đoạn hoàn thiện)
- Microservices: **tùy chọn sau MVP**, không bắt buộc để hoàn thành project

Không cần domain/VPS/API trả phí; chạy trên máy cá nhân. Tài nguyên Docker/SQL Server có thể tiêu thụ RAM đáng kể.

## Cấu trúc repo

```text
request-management-system/
├── AGENTS.md                   # Quy tắc và ngữ cảnh cho Codex
├── README.md
├── backend/
│   └── RequestManagementSystem.Api/  # ĐÃ tạo từ webapi template
├── frontend/                  # CHƯA tạo; React/Vite ở milestone sau
├── tests/
│   └── RequestManagementSystem.Tests/  # xUnit integration tests
└── docs/
    ├── 01-PRODUCT.md
    ├── 02-STACK-ARCHITECTURE.md
    ├── 03-DATA-MODEL.md
    ├── 04-API-CONTRACT.md
    ├── 05-MILESTONES.md
    ├── 06-TEST-STRATEGY.md
    ├── 07-DEV-WORKFLOW.md
    ├── 08-SECURITY-AND-QUALITY.md
    ├── 09-PROJECT-STATUS.md
    ├── 10-DECISIONS.md
    ├── 11-CODEX-HANDOFF.md
    └── 12-CODEX-PROMPTS.md
```

Danh sách trên là **cấu trúc dự kiến**; các thư mục chưa tồn tại không cần tạo trước thời điểm milestone yêu cầu.

## Thiết lập backend và database local

Yêu cầu:

- .NET SDK 10
- SQL Server local đang chạy; hướng dẫn này dùng default instance tại `localhost`
- Windows account hiện tại có quyền kết nối và tạo database

Từ **thư mục gốc repo**, khôi phục NuGet packages và local tool `dotnet-ef`:

```powershell
dotnet restore backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
dotnet tool restore
```

Lưu connection string ngoài source control bằng .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=RequestManagementSystemDb;Trusted_Connection=True;TrustServerCertificate=True;" --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

Tạo/cập nhật database từ các EF Core migrations đã có trong repo:

```powershell
dotnet ef database update --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj --startup-project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

Database mới sẽ chưa có request demo. Project không tự động seed request khi API khởi động. Sau khi ba demo user đã tồn tại, có thể chủ động tạo dữ liệu giả local bằng:

```powershell
dotnet run --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj -- --seed-demo-data 10000
```

Seeder ghi theo batch, tạo history phù hợp với status và bỏ qua nếu dữ liệu `[DEMO-LOAD]` đã tồn tại. Số lượng cho phép từ 1 đến 50.000; dữ liệu SQL Server local không được commit vào Git.

## Chạy API hiện có

Từ **thư mục gốc repo**:

```powershell
dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
dotnet watch run --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

Mở URL/port mà terminal in ra và gọi `GET /api/requests`. Endpoint này đọc dữ liệu từ SQL Server qua `RequestService` và EF Core.

## Chạy tests

```powershell
dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj
```

Integration test của M1 dùng SQLite in-memory với dữ liệu cách ly; test không đọc hoặc thay đổi `RequestManagementSystemDb` trên SQL Server local.

Concurrency test dùng SQL Server thật và tạo một database tạm có tên riêng. Từ PowerShell, lấy connection string local từ User Secrets mà không in giá trị ra màn hình, chạy test, rồi xóa biến môi trường:

```powershell
$connectionLine = dotnet user-secrets list --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj |
  Where-Object { $_ -match '^ConnectionStrings:DefaultConnection = ' } |
  Select-Object -First 1

$env:RMS_SQLSERVER_TEST_CONNECTION = ($connectionLine -split ' = ', 2)[1]

dotnet test tests/RequestManagementSystem.SqlServerTests/RequestManagementSystem.SqlServerTests.csproj

Remove-Item Env:RMS_SQLSERVER_TEST_CONNECTION
```

Test này không dùng hoặc xóa `RequestManagementSystemDb`; database tạm có tiền tố `RequestManagementSystemConcurrencyTests_` và được xóa trong `finally`.

## Quy tắc làm việc

1. **Mỗi lần một nhiệm vụ nhỏ**; không yêu cầu Codex xây toàn bộ phần mềm trong một prompt.
2. Trước khi code, **đọc `AGENTS.md` và `docs/09-PROJECT-STATUS.md`**, xác minh code thực tế.
3. Sau mỗi thay đổi backend: `dotnet build`, chạy test phù hợp, kiểm tra endpoint và cập nhật trạng thái.
4. Tài liệu ghi kế hoạch; **code trong repo và tests là nguồn xác nhận những gì đã chạy**.
5. Không công khai mật khẩu, JWT key, `.env`, secrets; không commit dữ liệu thật.

## Docs map

- Làm gì & không làm gì: `01-PRODUCT.md`
- Tổ chức code, stack và microservices: `02-STACK-ARCHITECTURE.md`
- Các entity: `03-DATA-MODEL.md`
- REST endpoints, examples: `04-API-CONTRACT.md`
- Milestones & tiêu chí hoàn thành: `05-MILESTONES.md`
- Test strategy: `06-TEST-STRATEGY.md`
- Quy trình dev: `07-DEV-WORKFLOW.md`
- Security: `08-SECURITY-AND-QUALITY.md`
- Hiện trạng & việc tiếp theo: `09-PROJECT-STATUS.md`
- Quyết định kỹ thuật: `10-DECISIONS.md`
- Cách duy trì ngữ cảnh cho Codex: `11-CODEX-HANDOFF.md`
- Prompt sẵn để bắt đầu session mới: `12-CODEX-PROMPTS.md`
