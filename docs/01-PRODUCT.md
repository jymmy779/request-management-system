# 01 — Product Requirements (PRD)

## Tổng quan

**Tên:** Request Management System  
**Loại:** Internal Request & Approval Management Web Application  
**Mục tiêu học tập:** C# / ASP.NET Core Web API, SQL, EF Core, JWT, authorization, testing, Docker, mở rộng Microservices nếu còn thời gian.  
**Người phát triển:** Solo; budget 0đ; tận dụng VS Code, Codex, máy cá nhân.  
**Đối tượng sử dụng mô phỏng:** Doanh nghiệp nhỏ, phòng ban nội bộ, CLB/lab.

## Vấn đề

Yêu cầu nội bộ thường đi qua tin nhắn/email rời rạc, khó biết ai gửi, ai duyệt, đang ở bước nào, vì sao từ chối. Ứng dụng tập trung yêu cầu và lưu lịch sử ra quyết định.

## Vai trò

| Vai trò | Quyền chính |
|---|---|
| Employee | Tạo yêu cầu, xem yêu cầu của mình, hủy yêu cầu còn Pending |
| Manager | Xem yêu cầu, duyệt/từ chối Pending, đánh dấu Completed sau Approved; không tự duyệt yêu cầu do mình tạo |
| Admin | Xem mọi yêu cầu, quản lý tài khoản/role (giai đoạn sau), có quyền xử lý như Manager; không tự duyệt yêu cầu do mình tạo |

Trong bản demo, có thể seed 3 tài khoản giả để diễn tập vai trò; không tích hợp nhân sự thật hay email công ty.

## Use case chính (MVP)

1. Employee đăng nhập.
2. Tạo yêu cầu với `title`, `description`, `category`, `priority`, `quantity` và tổng chi phí dự kiến tùy chọn.
3. Hệ thống lưu yêu cầu ở trạng thái `Pending`, gắn `requesterId` từ token.
4. Manager xem danh sách, mở chi tiết và duyệt hoặc từ chối (từ chối cần lý do).
5. Nếu Approved, Manager/Admin đánh dấu Completed khi đã xử lý.
6. Employee xem trạng thái và lịch sử thay đổi của yêu cầu mình gửi.
7. Employee được hủy (`Cancelled`) khi yêu cầu vẫn Pending.

## Trạng thái và chuyển trạng thái

```text
(New) -> Pending ──approve──> Approved ──complete──> Completed
                    ├─reject──> Rejected
                    └─cancel (owner)──> Cancelled
```

- Không được duyệt 2 lần, từ chối sau khi đã hoàn thành, hoặc complete khi chưa approved.
- Các thao tác thay đổi trạng thái phải ghi `RequestStatusHistory` và thời gian UTC.
- Không cho Manager/Admin tự duyệt yêu cầu do chính họ gửi.
- Khi hai manager xử lý cùng lúc, chỉ **một** thay đổi hợp lệ được chấp nhận; còn lại nhận lỗi conflict.

## Functional requirements

| ID | Yêu cầu | Ưu tiên |
|---|---|---|
| F01 | Đăng nhập; truy xuất người dùng hiện tại | Must |
| F02 | Tạo yêu cầu | Must |
| F03 | Xem danh sách và chi tiết theo quyền | Must |
| F04 | Duyệt / từ chối / hoàn tất / hủy theo state machine | Must |
| F05 | Lưu và xem lịch sử trạng thái | Must |
| F06 | Validation và HTTP errors thống nhất | Must |
| F07 | Dashboard tối giản theo vai trò | Should |
| F08 | Quản lý người dùng qua UI | Could |
| F09 | Thông báo async / microservice | Later |
| F10 | Tìm kiếm, lọc, sắp xếp và phân trang danh sách yêu cầu | Must |

**Category** MVP: `Equipment`, `Software`, `Other`. Chưa làm quy trình riêng cho từng loại, chưa quản lý tồn kho hay mua sắm thực tế.

## Phạm vi giao diện tham khảo

Thiết kế AdminHub được dùng để định hướng bố cục, màu sắc, sidebar, trang chi tiết và cách trình bày luồng phê duyệt. Thiết kế này **không phải yêu cầu triển khai giống 1:1**.

Trong MVP, frontend tập trung vào:

- Đăng nhập/đăng xuất và khôi phục phiên đăng nhập.
- Danh sách, tìm kiếm, lọc, sắp xếp và phân trang yêu cầu.
- Tạo, sửa và xem chi tiết yêu cầu.
- Hiển thị người gửi, phòng ban, priority, quantity, estimated cost và lịch sử trạng thái.
- Thao tác approve, reject, complete và cancel theo quyền hiện có.
- Trang My Approvals và dashboard thống kê tối giản nếu phần luồng chính đã ổn định.
- Loading, error, empty state và toast phản hồi thao tác.

Sidebar trong thiết kế có thể hiển thị ít mục hơn. Những mục chưa có nghiệp vụ backend không được tạo chỉ để giống ảnh.

## Non-functional requirements

- API sử dụng JSON, REST conventions, status code chính xác, validation rõ ràng.
- Không lộ dữ liệu giữa các nhân viên; server-side authorization trên tất cả request.
- Các thay đổi trạng thái có audit trail, nhất quán dữ liệu.
- Migrations có thể chạy lại; seed dữ liệu giả dành cho development.
- Unit/integration tests cho hành vi nghiệp vụ; CI chạy được trên repo.
- Có tài liệu cách chạy local, screenshot/demo GIF hoặc video tùy thời gian.
- Không có đòi hỏi số lượng người dùng lớn hoặc SLA production.

## Không làm trong MVP

- Payroll, chấm công, HRMS/ERP đầy đủ, kho, mua hàng/thanh toán.
- Multi-tenant, phân cấp duyệt tùy biến nhiều cấp, workflow designer.
- Chuỗi duyệt cố định Department → Manager → Director → VP; MVP giữ một bước xử lý bởi Manager/Admin.
- Vendor, policy, báo cáo/analytics chuyên sâu và các module Purchase/Travel/IT/Finance độc lập.
- Save draft, request changes, comment thread, attachment và budget center/contract period.
- App mobile, AI chatbot, push notification, email/SMS thật, external integrations.
- Kubernetes, distributed tracing hay microservices từ đầu.

## Tiêu chí thành công

Một người có thể clone repo, làm theo README, chạy API + database; đăng nhập bằng tài khoản giả; thực hiện đủ vòng đời yêu cầu; các trường hợp sai quyền/sai trạng thái được chặn; tests pass. Frontend minh họa được flow, không cần UI cầu kỳ.
