# 03 — Data Model (planned)

> Đây là thiết kế **dự kiến** khi tích hợp EF Core. `Models/Request.cs` hiện đã có phiên bản mẫu chưa gắn DB. Không được tự động thay toàn bộ model mà chưa đến milestone DB.

## Users

Được bổ sung ở **M2** qua ASP.NET Core Identity. `Id` kiểu string (mặc định Identity), email/username, password hash, role `Employee | Manager | Admin`, trạng thái tài khoản. Không tự lưu mật khẩu raw.

## Requests

M1 chỉ cần lưu các trường mẫu hiện có vào SQL Server để học EF Core; **M2 mới bổ sung `RequesterId`/Identity và liên kết lại dữ liệu demo** trước khi bật ownership filtering. Bảng bên dưới là trạng thái dữ liệu **sau M2–M4**, không phải schema bắt buộc phải tạo nguyên khối ở M1.

| Field | Type gợi ý | Required | Ghi chú |
|---|---|---|---|
| Id | int identity | Yes | Primary key |
| Title | nvarchar(150) | Yes | 5–150 ký tự |
| Description | nvarchar(2000) | Yes | 10–2000 ký tự |
| Category | enum-as-string | Yes | Equipment / Software / Other |
| Status | enum-as-string | Yes | Pending / Approved / Rejected / Completed / Cancelled |
| RequesterId | string FK -> Users | Yes | Lấy từ JWT claims, không nhận từ request body |
| CreatedAt | datetime2 UTC | Yes | Ngày tạo |
| UpdatedAt | datetime2 UTC | Yes | Ngày sửa lần cuối |
| DecidedAt | datetime2 UTC | No | Khi duyệt/từ chối |
| DecidedByUserId | string FK -> Users | No | Người phê duyệt/từ chối |
| RejectionReason | nvarchar(500) | No | Bắt buộc khi reject |
| RowVersion | rowversion | When implementing concurrency | Optimistic concurrency token của SQL Server |

### Yêu cầu về dữ liệu

- Trạng thái mặc định `Pending`.
- Index ít nhất `(RequesterId, CreatedAt)` và `(Status, CreatedAt)` nếu cần lọc.
- Không hard-delete các yêu cầu đã phê duyệt; chỉ cho hủy từ Pending.
- Tất cả thời gian lưu UTC; frontend chuyển sang local time khi hiển thị.
- `RowVersion` hoặc conditional update/transation để chống duyệt đồng thời. Dùng concurrency test trên SQL Server, không dựa hoàn toàn vào EF in-memory provider.

## RequestStatusHistory

| Field | Type | Ý nghĩa |
|---|---|---|
| Id | long identity | PK |
| RequestId | int FK | Yêu cầu được thay đổi |
| FromStatus | string nullable | Giá trị trước (null khi tạo) |
| ToStatus | string | Giá trị mới |
| ChangedByUserId | string FK | Người thực hiện |
| Note | string nullable | Lý do từ chối hoặc ghi chú |
| ChangedAt | datetime2 UTC | Thời điểm |

Lưu request status update và history entry **cùng transaction**.

## Quan hệ

```text
User (1) ----< Request (many)       [RequesterId]
User (1) ----< RequestStatusHistory  [ChangedByUserId]
Request (1) ----< RequestStatusHistory (many)
```

## EF Core notes

- Database migrations được quản lý bằng EF Core (không chỉnh DB thủ công rồi bỏ migration).
- Seed demo role/accounts cho development, cấu hình qua environment variables, không đưa credentials thật vào Git.
- Ban đầu có thể dùng `string Status` trong mẫu đã tạo; khi chuyển sang domain model, dùng enum + mapping rõ ràng, thống nhất JSON output.
- Tránh mô hình hóa thêm Departments, PurchaseOrders, Inventory... ở MVP vì chưa có nghiệp vụ tương ứng.
