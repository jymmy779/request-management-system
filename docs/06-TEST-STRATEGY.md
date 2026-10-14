# 06 — Testing Strategy

## Nguyên tắc

Tất cả **logic không liên quan UI/UX** phải có test thích hợp khi được bổ sung. Code mới phải build trước khi coi như xong. Manual UI testing do developer thực hiện. Không nói “tests pass” khi chưa chạy.

## Các cấp độ

1. **Unit tests (xUnit)**: business rules / validation / state machine; mock/stub dependency khi cần, không test `new List<Request>` hardcode vô nghĩa.
2. **Integration tests**: HTTP endpoints, DI, auth, EF Core data access. `WebApplicationFactory` cho API test; DB riêng dành cho tests, reset giữa test cases.
3. **Manual smoke**: Postman/browser và React UI. Codex cung cấp checklist nếu không thể thao tác UI.
4. **Concurrency tests**: chạy hai lệnh approve đồng thời và xác nhận chỉ một thao tác thành công; ưu tiên SQL Server thật/test container để sát production DB semantics.

## Bộ test tối thiểu theo milestone

| Milestone | Test cần có |
|---|---|
| M0 | Build + GET smoke. Chưa có nghiệp vụ để test unit hữu ích. |
| M1 | GET từ DB; seed/migration; dữ liệu tồn tại qua restart |
| M2 | Login success/fail, no token 401, insufficient role 403, owner isolation on GET list |
| M3 | Create 201, invalid 400, get by id 200/404 and non-owner protection |
| M4 | Approve/reject/complete/cancel hợp lệ; invalid transitions 409; self-approve denied; audit atomicity; concurrency |
| M5 | Backend regression + manual UI flow |
| M6 | CI restore/build/tests + setup reproducibility |
| M7 | Event published, consume once logically (idempotency), broker retry/error handling |

## Lệnh thường dùng

Từ **repo root**:

```powershell
dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj
```

Sau khi test project đã được tạo (đường dẫn cần xác nhận với repo):

```powershell
dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj
```

Nếu CI/test runner yêu cầu SQL Server mà không có DB đang chạy, cần báo rõ blocker, không bỏ qua rồi ghi "pass".

## Chiến lược data test

- Unit tests không phụ thuộc DB.
- Integration tests DB phải cách ly, không được xóa hoặc sửa database cá nhân.
- Không dùng `UseInMemoryDatabase` để khẳng định đúng concurrency/transactions của SQL Server.
- Nếu dùng SQLite in-memory cho test tốc độ, ghi rõ khác biệt và vẫn cần test tích hợp trên SQL Server cho concurrency/constraints.

## Test scenarios quan trọng cho M4

```text
Pending -> Approved -> Completed           OK
Pending -> Rejected                        OK (reason required)
Pending -> Cancelled (request owner)       OK
Approved -> Rejected                       409
Completed -> Approved                      409
Manager approves own request              403
Employee approves any request             403
Employee reads someone else's request     404 (chosen concealment strategy)
2 managers approve at the same time       1 success + 1 conflict
```

## Definition of tested

Ghi nhận `command`, `date`, `result`, `failures` trong `09-PROJECT-STATUS.md` sau mỗi milestone. Không tự ý sửa assertions chỉ để xanh khi behavior sai.
