# 04 — API Contract (planned REST v1)

**Base URL:** `/api` • JSON • Property casing: `camelCase` • Timestamp: UTC ISO-8601.  
**Lưu ý:** Hầu hết endpoint bên dưới **CHƯA được implement**; xem `09-PROJECT-STATUS.md`. `GET /api/requests` hiện có thể đang trả demo data.

## Auth (M2)

| Method | Route | Quyền | Kết quả |
|---|---|---|---|
| POST | `/api/auth/login` | Public | JWT access token + info tối thiểu |
| GET | `/api/auth/me` | Authenticated | current user: `id`, `email`, `role` |

Không mở public signup trong MVP; demo accounts tạo bằng dev seed. Không gửi password hash về client.

## Requests (M1–M4)

| Method | Route | Quyền | Ý nghĩa |
|---|---|---|---|
| GET | `/api/requests` | Authenticated | Employee: của mình; Manager/Admin: tất cả |
| GET | `/api/requests/{id}` | Owner / Manager / Admin | Chi tiết |
| POST | `/api/requests` | Authenticated | Tạo Pending |
| POST | `/api/requests/{id}/cancel` | Owner | Chỉ từ Pending |
| POST | `/api/requests/{id}/approve` | Manager/Admin, not owner | Pending -> Approved |
| POST | `/api/requests/{id}/reject` | Manager/Admin, not owner | Pending -> Rejected |
| POST | `/api/requests/{id}/complete` | Manager/Admin | Approved -> Completed |
| GET | `/api/requests/{id}/history` | Owner / Manager / Admin | Audit history |

Các endpoint thay đổi trạng thái **dùng POST action endpoints** để MVP dễ diễn đạt nghiệp vụ; không thêm generic `PATCH status` cho client tự chuyển trạng thái tùy ý.

### POST `/api/requests`

Request:

```json
{
  "title": "Request a new monitor",
  "description": "A second monitor is needed for development work.",
  "category": "Equipment"
}
```

Response `201 Created` với resource URL (`Location`):

```json
{
  "id": 12,
  "title": "Request a new monitor",
  "description": "A second monitor is needed for development work.",
  "category": "Equipment",
  "status": "Pending",
  "requesterId": "demo-employee-id",
  "createdAt": "2026-10-14T05:00:00Z"
}
```

Dữ liệu chỉ để minh họa contract. `requesterId` phải lấy từ JWT, **không** lấy từ body.

### POST `/api/requests/{id}/approve`

Body có thể để trống `{}`. Response: `200 OK` với request mới hoặc `204 No Content` (chọn một cách thống nhất khi implement; ưu tiên `200` để dễ demo UI).

### POST `/api/requests/{id}/reject`

```json
{
  "reason": "Budget not approved for this quarter."
}
```

`reason` bắt buộc, dài 5–500 ký tự.

## Errors

| HTTP | Khi nào |
|---|---|
| 400 | Validation sai, request body thiếu trường |
| 401 | Chưa đăng nhập/token không hợp lệ |
| 403 | Đăng nhập rồi nhưng không đủ quyền |
| 404 | Không tồn tại; hoặc che giấu request không được phép xem (chọn rule nhất quán) |
| 409 | Conflict do state transition không hợp lệ hoặc concurrent update |
| 500 | Lỗi ngoài dự kiến; log phía server, không tiết lộ stack trace production |

Dùng `ProblemDetails` của ASP.NET Core cho errors. **Không trả HTTP 200 kèm thông báo lỗi**.

## Pagination/filter (sau core)

Nếu danh sách dài, `GET /api/requests?page=1&pageSize=20&status=Pending`. Giới hạn page size, default sort `createdAt DESC`. Chưa cần ở API mẫu hiện tại.

## Manual demo scenario

1. Employee login -> `POST /api/requests` -> lấy `id`.
2. Manager login -> `GET /api/requests` -> approve.
3. Employee login -> `GET /api/requests/{id}` thấy `Approved`.
4. Manager complete -> xem history có 3 mốc Created/Approved/Completed.
5. Thử approve lại -> `409`.
6. Thử Employee approve -> `403`.
7. Thử Employee xem yêu cầu của người khác -> không được trả dữ liệu.
