# 02 — Tech Stack & Architecture

## Stack chuẩn

| Layer | Lựa chọn | Lý do |
|---|---|---|
| Backend | .NET 10 LTS, C#, ASP.NET Core Web API (Controllers) | Học .NET hiện đại, DI/HTTP pipeline |
| ORM | EF Core | Migrations, LINQ, relationships |
| Database | SQL Server Express/Developer **local** | Phổ biến với hệ sinh thái .NET; không phí cho học/dev |
| Authentication | ASP.NET Core Identity + JWT Bearer | Không tự viết thuật toán hash mật khẩu |
| Frontend | React + TypeScript + Vite | Tận dụng kiến thức sẵn có |
| Tests | xUnit + ASP.NET Core integration testing | Test business logic và HTTP layer |
| Tooling | VS Code, C# Dev Kit, Codex, Git, Postman | Không yêu cầu Visual Studio trả phí |
| Container | Docker Compose (sau khi core chạy) | API + DB chạy local, thuận tiện demo |
| Async (later) | RabbitMQ + .NET Worker Service | Thực hành eventual consistency |

Nếu máy không đủ RAM để chạy SQL Server container, dùng SQL Server Express trên Windows; Docker là **cải tiến sau**, không phải điều kiện học API đầu tiên.

## Nguyên tắc kiến trúc

- **Modular monolith trước:** một ASP.NET Core API, một SQL Server database.
- **Tách trách nhiệm:** Controller mỏng (HTTP + authorization), Service nắm nghiệp vụ, EF Core DbContext truy cập dữ liệu.
- DTO cho input/output, không trả toàn bộ entity khi có thông tin nhạy cảm.
- Constructor DI (dễ học) và đăng ký lifetime qua `Program.cs`.
- Dùng `async/await` cho database I/O. `CancellationToken` khi có lý do.
- **Không bắt buộc Repository pattern** chỉ để bọc `DbContext` bằng CRUD vô nghĩa.
- Không cần Clean Architecture 4 projects hay CQRS từ đầu.

## Cấu trúc mục tiêu (chỉ tạo khi cần)

```text
backend/
└── RequestManagementSystem.Api/
    ├── Controllers/            # RequestsController, AuthController
    ├── Models/                 # Request, RequestStatusHistory, ...
    ├── DTOs/                   # CreateRequestDto, RequestResponseDto, ...
    ├── Services/               # RequestService, ApprovalService (if warranted)
    ├── Data/                   # AppDbContext, Migrations, DevelopmentSeeder
    ├── Authorization/          # policy/requirements (only if needed)
    ├── Program.cs
    └── appsettings.json
tests/
└── RequestManagementSystem.Tests/  # Khi tạo xUnit project
frontend/
└── ...                         # Tạo sau khi API + auth ổn
```

## Luồng request tiêu chuẩn

```text
Browser/Postman
  -> ASP.NET Core Middleware (exception handling, auth)
  -> Controller (route, input validation, response)
  -> Service (business rules, authorization of resource as needed)
  -> EF Core DbContext -> SQL Server
  -> Service result -> Controller -> JSON HTTP response
```

**Dòng hiện tại**: `RequestsController -> RequestService.GetAll -> hardcoded List<Request>`; chỉ thay bằng database khi bước tương ứng được yêu cầu.

## Boundaries trong code

- Không đưa `HttpContext`, status code vào Service nếu không thực sự cần.
- `GetAllAsync(user)` phải lọc theo quyền trước khi trả dữ liệu.
- Model hiện tại tên `Request` và đã tồn tại. Giữ nguyên tên ở giai đoạn đầu; dùng DTO/namespace đầy đủ nếu bị trùng với `ControllerBase.Request`.
- Chọn API errors rõ ràng (`400`, `401`, `403`, `404`, `409`) và nhất quán.

## Microservices — tùy chọn sau khi MVP ổn định

Chỉ làm khi đã có REST API, auth, database, tests, frontend và Docker ổn định.

```text
RequestManagementSystem.Api (owns Requests DB)
     |
     | publishes RequestApproved event (RabbitMQ)
     v
Notification.Worker (independent process, owns Notifications DB)
     |
     +--> in-app notification log; no external paid services
```

Một **background hosted service chạy trong cùng API** chưa phải microservice độc lập. Notification.Worker chỉ được coi là service độc lập khi có lifecycle/deploy riêng, data ownership và giao tiếp qua hợp đồng event.

Trong bản học tập: consumer phải idempotent theo `eventId`; retry khi lỗi tạm thời; tránh gửi notification trùng. Outbox là cải tiến nên làm nếu muốn tăng độ tin cậy; không tuyên bố hệ thống đảm bảo delivery exactly-once.
