# 04 — API Contract

**Base URL:** `/api` • JSON • Property casing: `camelCase` • Timestamp: UTC ISO-8601.  
**Lưu ý:** Hầu hết endpoint bên dưới **CHƯA được implement**; xem `09-PROJECT-STATUS.md`. `GET /api/requests` hiện có thể đang trả demo data.

## Auth (M2)

| Method | Route | Quyền | Kết quả |
|---|---|---|---|
| POST | `/api/auth/login` | Public | `204`; backend sets an HttpOnly access-token cookie |
| POST | `/api/auth/logout` | Public | `204`; backend removes the access-token cookie |
| GET | `/api/auth/me` | Authenticated | current user: `id`, `email`, `role`, `departmentName` |

Không mở public signup trong MVP; demo accounts tạo bằng dev seed. Không gửi password hash về client.

JWT chỉ được phát hành trong HttpOnly cookie; login không trả access token trong JSON. Browser clients phải gửi requests với credentials. Development dùng `SameSite=Lax`; cookie chỉ gắn `Secure` khi request chạy qua HTTPS.

## Requests (M1–M4)

| Method | Route | Quyền | Ý nghĩa |
|---|---|---|---|
| GET | `/api/requests` | Authenticated | Danh sách phân trang; Employee: của mình; Manager/Admin: tất cả |
| GET | `/api/requests/{id}` | Owner / Manager / Admin | Chi tiết |
| POST | `/api/requests` | Authenticated | Tạo Pending |
| PUT | `/api/requests/{id}` | Owner | Sửa nội dung khi còn Pending |
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
  "category": "Equipment",
  "priority": "High",
  "quantity": 3,
  "estimatedCost": 18000000
}
```

Response `201 Created` với resource URL (`Location`):

```json
{
  "id": 12,
  "title": "Request a new monitor",
  "description": "A second monitor is needed for development work.",
  "category": "Equipment",
  "priority": "High",
  "quantity": 3,
  "estimatedCost": 18000000,
  "status": "Pending",
  "requesterId": "demo-employee-id",
  "requesterFullName": "Demo Employee",
  "requesterEmail": "employee@example.local",
  "requesterDepartmentName": "Information Technology",
  "createdAt": "2026-10-14T05:00:00Z",
  "updatedAt": null
}
```

Dữ liệu chỉ để minh họa contract. `requesterId` phải lấy từ JWT, **không** lấy từ body. `updatedAt` là `null` khi request chưa từng được sửa hoặc đổi trạng thái; sau đó API trả thời điểm thay đổi gần nhất theo UTC.

### PUT `/api/requests/{id}`

Body dùng cùng các trường và giới hạn validation như khi tạo request: `title`, `description`, `category`, `priority`, `quantity`, `estimatedCost`. `priority` chỉ nhận `Low`, `Medium`, hoặc `High`; `quantity` phải là số nguyên từ 1 đến 1.000; `estimatedCost` là tổng chi phí VND tùy chọn và phải lớn hơn 0 khi được cung cấp. Chỉ owner được sửa và request phải còn `Pending`; non-owner nhận `403`, request không còn Pending hoặc bị cập nhật đồng thời nhận `409`.

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

## Request list query

`GET /api/requests` hỗ trợ:

- `page`: mặc định `1`, nhỏ nhất `1`.
- `pageSize`: mặc định `20`, từ `1` đến `100`.
- `search`: tối đa 100 ký tự, tìm trong title và description.
- `status`: `Pending`, `Approved`, `Rejected`, `Completed`, `Cancelled`.
- `category`: `Equipment`, `Software`, `Other`.
- `sortBy`: `createdAt`, `title`, `status`; mặc định `createdAt`.
- `sortDirection`: `asc` hoặc `desc`; mặc định `desc`.

Ví dụ:

```text
GET /api/requests?search=monitor&status=Pending&page=2&pageSize=20&sortBy=createdAt&sortDirection=desc
```

Response `200 OK`:

```json
{
  "items": [],
  "page": 2,
  "pageSize": 20,
  "totalCount": 53,
  "totalPages": 3
}
```

Search/filter và `totalCount` luôn được tính sau khi áp dụng quyền xem dữ liệu của user. Query parameter không hợp lệ trả `400 Bad Request`.

## Manual demo scenario

1. Employee login -> `POST /api/requests` -> lấy `id`.
2. Manager login -> `GET /api/requests` -> approve.
3. Employee login -> `GET /api/requests/{id}` thấy `Approved`.
4. Manager complete -> xem history có 3 mốc Created/Approved/Completed.
5. Thử approve lại -> `409`.
6. Thử Employee approve -> `403`.
7. Thử Employee xem yêu cầu của người khác -> không được trả dữ liệu.
