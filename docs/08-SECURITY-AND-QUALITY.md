# 08 — Security, Reliability & Quality Guardrails

## Identity/authorization

- Sử dụng ASP.NET Core Identity để hash password (không tự viết MD5/SHA256 password hashing).
- Dùng JWT Bearer validation: issuer/audience/signing key/expiry, HTTPS khi deploy.
- Server xác định `requesterId` từ claims, không tin userId/role từ JSON body.
- `[Authorize]` và kiểm tra **resource ownership** (chỉ dựa vào role là không đủ).
- No self-approval; client không được tự ghi status ngoài nghiệp vụ được phép.
- Không mở public signup trong demo MVP nếu chưa xây flow an toàn.

## Secrets

- Không hardcode DB passwords, JWT key, seeding credentials vào repo.
- Dùng `dotnet user-secrets` cho local; env variables cho Docker/CI.
- Demo seed user/pass chỉ cấu hình local; hướng dẫn tạo bằng biến môi trường.
- `.gitignore` bao gồm bin/obj/.env/.vs/ và secret files.

## Validation & HTTP

- Title 5–150, Description 10–2000, Category trong enum.
- Reject cần Reason 5–500. Client error dùng 400/401/403/404/409 đúng semantics.
- Không lộ internal exception details khi production; log server-side.
- API trả DTO, không serialize whole IdentityUser hay secrets.

## Reliability & database

- Update request + status history trong cùng transaction.
- Tránh race condition approve/reject dùng optimistic concurrency (`rowversion`) hoặc atomic conditional update.
- Conflict trả 409; không cố retry silently việc duyệt vì có thể làm sai nghiệp vụ.
- Idempotency chỉ cần triển khai ở các lệnh/event thực sự có nguy cơ gửi lại; microservice consumer phải chống duplicate theo eventId.

## Performance

- Queries dùng `.AsNoTracking()` cho read-only khi phù hợp.
- Danh sách có pagination nếu dữ liệu lớn; không tối ưu sớm.
- Không cần Redis, distributed cache, queue hoặc event bus ở MVP core.

## Operational readiness

- `/health` optional khi docker/CI được thêm.
- Structured logging vừa đủ, không log passwords/JWT/PII.
- CORS whitelist React local origin chính xác, không allow `*` với credentials.
- Build/test có thể tái lập, docs không nói đã deploy nếu chưa.
