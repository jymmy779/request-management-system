# 12 — Codex Prompts (copy/paste)

## A. First prompt to Codex **NOW** (M0)

```text
Hãy đọc lần lượt AGENTS.md, README.md, docs/09-PROJECT-STATUS.md,
docs/05-MILESTONES.md và docs/11-CODEX-HANDOFF.md trong repo này.

Sau đó kiểm tra CODE THẬT ở backend/RequestManagementSystem.Api:
- Program.cs
- Controllers/ (đặc biệt RequestsController / RequestsControllers)
- Services/RequestService.cs
- Models/Request.cs

Mình là người mới với C#/.NET nhưng đã biết Java, JS/Express và OOP.
Chỉ làm M0: bảo đảm GET /api/requests gọi qua constructor-injected
RequestService.GetAll() và trả về List<Request> dữ liệu demo.

Nếu đã chạy đúng thì KHÔNG sửa code chỉ để sửa; báo kết quả kiểm tra.
Nếu có lỗi, sửa tối thiểu. Không thêm database, auth, frontend hay microservices.
Chạy dotnet build, kiểm tra endpoint nếu môi trường cho phép.
Giải thích thay đổi C# ngắn gọn bằng tiếng Việt; ghi chính xác file nào sửa.
Cuối cùng cập nhật docs/09-PROJECT-STATUS.md với kết quả đã xác minh,
còn gì chưa xác minh, và chỉ MỘT việc nên làm tiếp.
```

## B. Generic prompt for the **next small task**

```text
Đọc AGENTS.md, docs/09-PROJECT-STATUS.md và milestone hiện tại
trong docs/05-MILESTONES.md. Kiểm tra source/test thực tế trước.

Nhiệm vụ duy nhất lần này: [MÔ TẢ MỘT TASK NHỎ].

Yêu cầu:
- Không làm thêm tính năng ngoài phạm vi task.
- Giữ kiến trúc đơn giản, đúng conventions của ASP.NET Core.
- Nếu có business logic mới, bổ sung tests phù hợp.
- Chạy build/test và báo rõ lệnh + kết quả thật.
- Nêu ngắn gọn kiến thức C#/.NET mình cần hiểu.
- Cập nhật docs/09-PROJECT-STATUS.md và các docs liên quan nếu có thay đổi.
- Đề xuất đúng 1 next task, không tự thực hiện.
```

## C. Milestone review prompt

```text
Hãy review milestone hiện tại theo docs/05-MILESTONES.md và AGENTS.md.
Đối chiếu acceptance criteria với code/tests, không chỉ dựa vào STATUS.
Chạy build và tất cả tests liên quan; kiểm tra edge cases, security,
contract API, code quality, rồi liệt kê PASS/FAIL/NOT VERIFIED.
Nếu có lỗi, đề xuất bản sửa nhỏ nhất; không tự nhảy milestone.
Cập nhật docs/09-PROJECT-STATUS.md sau khi mình xác nhận.
```

## D. New session recovery prompt

```text
Đây là session Codex mới. Đừng dùng trí nhớ từ chat cũ.
Đọc AGENTS.md, README.md, docs/09-PROJECT-STATUS.md,
docs/11-CODEX-HANDOFF.md và các docs liên quan.
Tự kiểm tra source code + git status + project build/test hiện tại.
Hãy tóm tắt tối đa 8 dòng: mục tiêu project, stack, trạng thái thực tế,
blockers, next 1 task. CHƯA sửa code cho đến khi mình đồng ý.
```


TUYỆT ĐỐI KHÔNG THẢO MAI!!!!!!!!