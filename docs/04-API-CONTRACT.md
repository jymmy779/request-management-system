# 04 — API Contract

**Base URL:** `/api` • JSON • Property casing: `camelCase` • Timestamp: UTC ISO-8601.  
**Lưu ý:** Hầu hết endpoint bên dưới **CHƯA được implement**; xem `09-PROJECT-STATUS.md`. `GET /api/requests` hiện có thể đang trả demo data.

## Auth (M2)

| Method | Route | Quyền | Kết quả |
|---|---|---|---|
| POST | `/api/auth/login` | Public | `204`; backend sets an HttpOnly access-token cookie |
| POST | `/api/auth/logout` | Public | `204`; backend removes the access-token cookie |
| GET | `/api/auth/me` | Authenticated | current user: `id`, `fullName`, `email`, `role`, `departmentName` |
| POST | `/api/auth/change-password` | Authenticated | Đổi mật khẩu bằng current/new password; thành công xóa cookie hiện tại |
| POST | `/api/auth/forgot-password` | Public | Luôn `202`; gửi reset link nếu tài khoản email hợp lệ và không tiết lộ email có tồn tại |
| POST | `/api/auth/reset-password` | Public + reset token | Xác minh email/token và đổi sang mật khẩu mới; thành công `204` |

Không mở public signup trong MVP; demo accounts tạo bằng dev seed. Không gửi password hash về client.

JWT chỉ được phát hành trong HttpOnly cookie; login không trả access token trong JSON. Browser clients phải gửi requests với credentials. Development dùng `SameSite=Lax`; cookie chỉ gắn `Secure` khi request chạy qua HTTPS.

`POST /api/auth/change-password` yêu cầu `currentPassword` và `newPassword`. Mật khẩu mới phải khác mật khẩu hiện tại và vượt qua Identity password policy. Thành công trả `204` và xóa access-token cookie của browser hiện tại để buộc đăng nhập lại. JWT đã phát hành cho thiết bị khác chưa được thu hồi trong MVP.

`POST /api/auth/forgot-password` nhận `email` và luôn trả `202 Accepted` với body rỗng cho một email đúng định dạng, kể cả khi tài khoản không tồn tại hoặc email không gửi được. Nếu tài khoản tồn tại và email đã xác nhận, reset token chỉ xuất hiện trong link gửi qua email; API không trả token. Token Identity hiện có thời hạn 30 phút.

`POST /api/auth/reset-password` nhận `email`, Base64Url `token` từ reset link và `newPassword`. Email không tồn tại, token sai, hết hạn hoặc đã dùng đều trả cùng lỗi `400`; mật khẩu mới không đạt Identity policy trả validation error. Thành công trả `204`, không tự đăng nhập user và token cũ không thể dùng lại.

## Requests (M1–M4)

| Method | Route | Quyền | Ý nghĩa |
|---|---|---|---|
| GET | `/api/requests` | Authenticated | Danh sách phân trang; Employee: của mình; Manager/Admin: tất cả |
| GET | `/api/requests/my-approvals` | Manager / Admin | Hàng đợi Pending mà user hiện tại có thể duyệt, loại request do chính họ tạo |
| GET | `/api/requests/approval-history` | Manager / Admin | Các request mà user hiện tại đã approve hoặc reject |
| GET | `/api/requests/{id}` | Owner / Manager / Admin | Chi tiết |
| POST | `/api/requests` | Authenticated | Tạo Pending |
| PUT | `/api/requests/{id}` | Owner | Sửa nội dung khi còn Pending |
| POST | `/api/requests/{id}/cancel` | Owner | Chỉ từ Pending |
| POST | `/api/requests/{id}/approve` | Manager/Admin, not owner | Pending -> Approved |
| POST | `/api/requests/{id}/reject` | Manager/Admin, not owner | Pending -> Rejected |
| POST | `/api/requests/{id}/complete` | Manager/Admin | Approved -> Completed |
| GET | `/api/requests/{id}/history` | Owner / Manager / Admin | Audit history |

Các endpoint thay đổi trạng thái **dùng POST action endpoints** để MVP dễ diễn đạt nghiệp vụ; không thêm generic `PATCH status` cho client tự chuyển trạng thái tùy ý.

## Departments (M5)

| Method | Route | Quyền | Ý nghĩa |
|---|---|---|---|
| GET | `/api/departments` | Employee / Manager / Admin | Danh sách phòng ban tham chiếu, sắp xếp theo tên |

Response `200 OK`:

```json
[
  {
    "id": 3,
    "code": "FIN",
    "name": "Finance"
  }
]
```

Phòng ban của một request được suy ra từ requester. Client không được tự gửi `departmentId` khi tạo hoặc sửa request.

## Users (M5)

| Method | Route | Quyền | Ý nghĩa |
|---|---|---|---|
| GET | `/api/users` | Admin | Danh sách user phân trang kèm role và phòng ban |
| POST | `/api/users` | Admin | Tạo tài khoản, hash password bằng Identity, gán một role và phòng ban tùy chọn |

`GET /api/users` hỗ trợ `page`, `pageSize`, `search`, `role`, `departmentId`, `sortBy=fullName|email` và `sortDirection=asc|desc`. `role` chỉ nhận `Employee`, `Manager`, hoặc `Admin`. Response dùng `PagedResult<UserResponseDto>` và không bao giờ chứa password hash hoặc thông tin xác thực nhạy cảm.

`POST /api/users` nhận:

```json
{
  "fullName": "Nguyen Van A",
  "email": "nguyenvana@example.local",
  "password": "Strong!Password123",
  "role": "Employee",
  "departmentId": 1
}
```

`departmentId` có thể là `null`. Password phải vượt qua policy của ASP.NET Core Identity và chỉ được lưu dưới dạng hash. Email trùng trả `409`; password không hợp lệ, role không hợp lệ hoặc department không tồn tại trả `400`. Response `201` dùng `UserResponseDto` và không trả password.

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
- `priority`: `Low`, `Medium`, `High`.
- `departmentId`: ID phòng ban của requester, phải là số nguyên dương; ownership scope vẫn được áp dụng trước khi lọc.
- `sortBy`: `createdAt`, `title`, `status`; mặc định `createdAt`.
- `sortDirection`: `asc` hoặc `desc`; mặc định `desc`.

Ví dụ:

```text
GET /api/requests?search=monitor&status=Pending&priority=High&departmentId=3&page=2&pageSize=20&sortBy=createdAt&sortDirection=desc
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

`GET /api/requests/my-approvals` dùng cùng query parameters và cùng cấu trúc `PagedResult<RequestResponseDto>`. Trong MVP chưa có bảng phân công approver, vì vậy “My Approvals” nghĩa là **hàng đợi request mà Manager/Admin hiện tại có quyền xử lý**: request phải còn `Pending` và không do chính họ tạo. Endpoint này không tuyên bố request đã được giao đích danh cho một manager cụ thể.

`GET /api/requests/approval-history` cũng dùng cùng query parameters và `PagedResult<RequestResponseDto>`, nhưng chọn request dựa trên audit history: phải tồn tại một lần chuyển sang `Approved` hoặc `Rejected` do user hiện tại thực hiện. Vì vậy request đã được user duyệt rồi chuyển tiếp sang `Completed` vẫn xuất hiện trong lịch sử quyết định của họ.

## Manual demo scenario

1. Employee login -> `POST /api/requests` -> lấy `id`.
2. Manager login -> `GET /api/requests` -> approve.
3. Employee login -> `GET /api/requests/{id}` thấy `Approved`.
4. Manager complete -> xem history có 3 mốc Created/Approved/Completed.
5. Thử approve lại -> `409`.
6. Thử Employee approve -> `403`.
7. Thử Employee xem yêu cầu của người khác -> không được trả dữ liệu.
