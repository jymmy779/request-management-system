# 07 — Local Development & Team-like Workflow

## Môi trường

- Windows + VS Code + extension **C# Dev Kit** và Codex.
- .NET SDK 10 đã được người dùng cài và chạy template thành công (theo hội thoại).
- Postman hoặc HTTP client; Git + GitHub.
- EF CLI, SQL Server, Node.js và Docker chỉ cài khi đến milestone tương ứng.

## Lệnh dùng hằng ngày

Chạy từ root repo:

```powershell
dotnet watch run --project backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

- `dotnet watch` thường reload/khởi động lại khi code thay đổi.
- Đang chạy `dotnet run` thì `Ctrl + C` trước khi mở `dotnet watch` để tránh tranh port.
- Port tùy output của terminal; **không hardcode port** trong tài liệu.

Build:

```powershell
dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

Smoke test: mở `<base-url>/api/requests`. Chỉ sau khi M2 triển khai auth, endpoint này cần Bearer JWT.

## Convention

- PascalCase: class, interface, method, property (`RequestsController`, `RequestService`, `GetAllAsync`).
- camelCase: local variable/argument (`requestService`, `requestId`).
- Private field: `_camelCase` (`_requestService`).
- Một public class/file, namespace trùng project structure khi phù hợp.
- Controllers tên số nhiều với hậu tố `Controller`: `RequestsController.cs`.
- Service trả DTO/domain result; Controller chịu trách nhiệm HTTP status codes.
- Dùng `Async` suffix cho async methods, không gọi `.Result`/`.Wait()` trong request path.
- `appsettings.json` chỉ dành cho cấu hình không nhạy cảm; secrets qua User Secrets/env.

## Git workflow gợi ý (solo, vẫn giống công ty)

```text
main
  └── feature/m1-efcore-database
  └── feature/m2-auth-rbac
  └── feature/m3-create-request
```

- Một task = một branch/commit nhỏ. PR mô tả: What / Why / How tested.
- `dotnet build` + tests rồi mới merge.
- Cập nhật STATUS sau milestone; cập nhật DECISIONS khi thay đổi quyết định.
- Không commit `bin/`, `obj/`, `.vs/`, `.env`, User Secrets, secret keys, logs, DB data hoặc credentials thật.

## Người học cần tự làm

- Đọc được class và constructor DI.
- Giải thích đường đi HTTP request qua controller/service/DbContext.
- Hiểu `async/await` và LINQ truy vấn.
- Có thể trả lời tại sao chọn status code và thiết kế role check.
- Không chỉ copy code từ Codex: kiểm tra diff và chạy tests.

## Nếu Codex bị mất ngữ cảnh

Mở `docs/11-CODEX-HANDOFF.md` và dùng prompt trong `docs/12-CODEX-PROMPTS.md`. Codex phải đọc source hiện tại trước khi sửa; tài liệu có thể bị cũ.
