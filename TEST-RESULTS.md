# Kết quả kiểm thử

Ngày thực hiện: 29/10/2026. Môi trường: Windows, .NET 10, SQL Server localhost/GameDB.

## Build và cú pháp
- dotnet build PlayEdu.csproj --no-restore: thành công, 0 warning, 0 error.
- node --check wwwroot/js/app.js: thành công.
- node --check scripts/e2e.mjs: thành công.

## HTTP và persistence
scripts/e2e.mjs: 19 kiểm tra thành công, bao gồm một kiểm tra Live thiếu cấu hình trên server thử riêng.

1. Landing, hướng dẫn, sample trả trang thành công.
2. Đăng ký hash mật khẩu, bỏ qua role/coin giả từ client, nhận 20 coin và ledger.
3. POST thiếu CSRF token bị chặn.
4. PDF được trích chữ và mở nguồn; user khác nhận 404 cho metadata/PDF.
5. Nâng PDF từ 15 tới 40 trang thu đúng 15 coin và ledger.
6. Thiếu coin: upload rollback, không thêm metadata.
7. Build demo lưu SQL, không thu coin.
8. Live thiếu cấu hình: rollback debit/ledger/kịch bản, giữ số dư, có AICall thất bại và CoinCharged=0.
9. Gửi lại lựa chọn cùng step không tạo lựa chọn thứ hai.
10. Chơi hết bốn tình huống, lưu Completed và chấm server; kết quả không mở được từ user khác.
11. Câu trả lời demo có sourceSlide được lưu SQL.
12. Lưu kho hai lần cùng run chỉ có một record.
13. Rating lưu SQL.
14. Practice chấm trên server, không nhúng đáp án JSON vào giao diện trước nộp.
15. Ví và hồ sơ render thành công với dữ liệu thật.
16. Admin console render; Admin bị chặn mở PDF người học.
17. Điều chỉnh coin và audit log đồng bộ.
18. Không cho điều chỉnh gây số dư âm.
19. User bị khóa mất quyền dùng cookie cũ ở request tiếp theo.

Script đã dọn records và PDF của chính lượt chạy, giữ nguyên seed/data sẵn có.

## Browser
- Trang chủ desktop hiển thị đúng bố cục.
- Kiểm tra chiều rộng 390px: không cuộn ngang.
- Sample: click một phương án khóa cả ba nút và hiện hậu quả; có nút đi tiếp.
- Form Login không còn khung validation trống.
- Không có console error/warning được ghi nhận trong các trang đã kiểm.
- Chưa kiểm thử visual mọi trang trên mọi kích thước.

## Chưa xác minh
- Provider AI thật: chưa có API key/model do người dùng cấu hình.
- Thanh toán thật/OAuth: chưa triển khai.
- E2E hiện kiểm tra HTTP và SQL, không thay cho kiểm thử tải cao/production.

