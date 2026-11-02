# EduPlay V2 — theo bộ màn hình tham chiếu

Dự án ASP.NET Core MVC hiện tại được mở rộng bằng module `/edu`, dùng SQL Server **EduPlayV2**. Module cũ vẫn dùng **GameDB**; không chuyển hoặc xóa dữ liệu cũ. Không dùng Sites hosting vì ứng dụng cần ASP.NET Core và SQL Server cục bộ.

## Chạy trên máy

Yêu cầu .NET 10 SDK, SQL Server 2019 trở lên, `sqlcmd` và Windows Authentication.

```powershell
./scripts/setup-edu.ps1
./scripts/start-edu.ps1
```

Mở http://localhost:5211/edu/login. Xem giao diện không cần tài khoản tại http://localhost:5211/edu/preview/today. Cổng 5211 tách khỏi bản cũ đang chạy tại 5210. Script dùng cấu hình Release để tránh ghi đè file Debug đang bị ứng dụng cũ giữ.

Connection string: `ConnectionStrings:EduPlayV2` trong `appsettings.json`, hoặc biến môi trường `ConnectionStrings__EduPlayV2`. Chứng chỉ tự ký chỉ được chấp nhận cho kết nối SQL phát triển cục bộ.

## Các màn đã dựng

| Nhóm | Đường dẫn sau `/edu/` |
| --- | --- |
| Tài khoản | `login`, `register`, `onboarding`, `admin-login` |
| Điều hướng học tập | `today`, `tools`, `courses`, `search` |
| Đọc và ôn bài | `slides`, `flashcards`, `quiz`, `links`, `case`, `case-results` |
| Kho và tiến độ | `vault`, `files`, `progress` |
| Coin, cài đặt | `wallet`, `topup`, `pricing`, `settings`, `states` |
| VIP xem trước | `video`, `plan`, `mock`, `study-pack`, `export` |
| Vận hành nội dung | `admin`, `admin-review`, `admin-reports`, `admin-gaps`, `admin-slides`, `admin-cases` |
| Super Admin | `admin-users`, `admin-pricing`, `admin-finance`, `admin-roles` |

Thêm `/preview/` trước tên màn để xem bố cục công khai. Preview không trả dữ liệu tài khoản, báo lỗi, giao dịch hoặc hoạt động quản trị. Nội dung mẫu thuộc thư viện công khai được ghi rõ là mẫu; case được gắn nhãn **giả định**, không giả làm case doanh nghiệp thật.

## Luồng có dữ liệu thật

- Đăng ký: hash mật khẩu, role Learner cố định, nhận 20 coin có ledger, tạo ba ô lưu và cài đặt riêng. Chọn môn/lịch học/ngày thi ở bước 2.
- Cookie riêng `EduPlay.V2.Session`, kiểm tra trạng thái và vai trò tài khoản trên mỗi request. Có chống CSRF và rate limit đăng nhập.
- Flashcard: lật thẻ, ghi nhận nhớ/chưa nhớ, lịch 10 phút hoặc 1–3–7–14 ngày.
- Tự kiểm tra: server giữ đáp án, chấm và lưu câu trả lời, kho câu sai, độ vững khái niệm, hoạt động theo ngày địa phương.
- Case bốn cấp: khóa thứ tự lựa chọn, lưu lượt chơi, một coin cho lần hoàn thành đầu tiên; chơi lại không thưởng trùng. Lưu kết quả vào ô và mua ô bổ sung (10 coin, tối đa 9).
- PDF riêng: kiểm tra chữ ký PDF, tối đa 20 MB/40 trang, trích xuất 15 trang đầu; chủ file mới được tải xuống. Mở trang bổ sung thu 15 coin nguyên tử, một lần mỗi file. Ghi chú theo user/slide.
- Tạo bộ học từ PDF: dùng adapter AI của hệ thống cũ. `AI:Mode=Demo` tạo thẻ trích đoạn và câu hỏi tra cứu nguồn, **không gọi AI và không thu coin**. Live có giới hạn 6 câu miễn phí/ngày rồi 1 coin/3 câu. Chưa kiểm thử provider Live vì chưa có API key.
- Cài đặt nhắc ôn và ghi nhớ, waitlist VIP được lưu trong SQL. Ví hiển thị số dư và lịch sử thực tế.
- Báo lỗi 48 giờ: người duyệt xác nhận/bác bỏ, có audit; xác nhận lỗi ẩn nội dung và thưởng 5 coin một lần. Duyệt/phát hành nội dung và nhận việc trong hàng đợi nội dung thiếu.
- Admin đăng nhập bằng email và mật khẩu; sai năm lần khóa 15 phút. Vai trò biên tập/duyệt/SuperAdmin tách riêng.

## Tạo quản trị viên

Sau khi build Release:

```powershell
./scripts/create-edu-admin.ps1 -Email 'admin@example.com'
```

Nhập mật khẩu ẩn. Script tạo tài khoản SuperAdmin đăng nhập bằng email và mật khẩu. Không cần Authenticator. Đăng nhập tại `/edu/login`. Sao lưu an toàn `App_Data/keys` cùng dữ liệu khi triển khai; mất khóa có thể khiến không giải mã được TOTP.

## Phần cần tích hợp thêm

- OAuth Google/Facebook/Microsoft, gửi email đặt lại mật khẩu và bộ gửi nhắc học. Giao diện thông báo rõ nếu dịch vụ chưa được cấu hình.
- Cổng MoMo/ZaloPay/VietQR/thẻ và webhook đã xác minh. Hiện không thu tiền hoặc tự cộng coin. SQL có `ConfirmPayment` và idempotency cho backend gateway tương lai; không cấp quyền thực thi cho client.
- Các chức năng VIP đúng trạng thái **sắp có**, có màn xem trước và đăng ký nhận thông báo, chưa sinh video, đề thi hoặc PDF thực tế.
- Nối khái niệm có ngân hàng quan hệ riêng, thời gian đếm ngược, đúng cộng 5 giây và sai kết thúc lượt. Chưa có bảng xếp hạng nhiều người.
- Danh mục MKT201/MKT304 chưa có chương thật; MKT101 có năm chương tham chiếu, nội dung học mẫu ở chương 4. Chưa nhập PDF giảng viên hay bài báo nguồn thật từ bên ngoài.
- Thu thập bài báo/case tự động và nhập nguồn tin thật chưa được cấu hình. Các form tải/duyệt slide, sửa giá, đổi vai trò, khóa tài khoản và điều chỉnh coin đã nối SQL, có kiểm tra quyền và audit.
- Thưởng chuỗi bảy ngày và giới thiệu bạn chưa tự động kích hoạt. Hạn mức AI ở ví hiện hiển thị chính sách, chưa hiển thị lượt còn lại từ bộ đếm.
- Chưa có OCR; PDF scan cần OCR trước. Xóa file hiện là ẩn mềm, chưa có tác vụ xóa vật lý theo lịch.
- Hôm nay lấy môn/chương đã chọn; nút ôn chương lọc nội dung theo chương. Bản preview dùng MKT101/Chương 4 mẫu. Chưa có bộ lập lịch gợi ý tự động từ toàn bộ lịch học.

Các mục này được ghi rõ để phân biệt giao diện đã dựng, luồng đã chạy và tích hợp chưa có dịch vụ ngoài.

## Database và giả định

`Database/V2/001_CreateDatabase.sql`: schema mới với khóa ngoại, check, index và UTC timestamps. `002`: trường, ngành, chương, vai trò, giá và chính sách. `003`: thủ tục coin/thanh toán và view. `004`: nội dung mẫu nguyên gốc. `005`: cột TOTP cũ (không còn được dùng khi đăng nhập). `006`: ngân hàng quan hệ khái niệm.

Chọn **3 ô lưu chỉ cho kết quả case**, PDF riêng dùng kho riêng. Giới hạn PDF giữ mức của hệ thống cũ: **20 MB, 40 trang**. VIP chưa có giá. Những chỗ này có thể thay đổi sau khi chốt nghiệp vụ.

Không tự chuyển tài khoản/mật khẩu/PDF/lượt chơi từ GameDB. Khi cần di chuyển phải có bảng ánh xạ và kiểm tra số dư trước khi đổi ứng dụng cũ sang schema mới.

## Kiểm thử

```powershell
dotnet build PlayEdu.csproj -c Release -p:UseAppHost=false
node --check wwwroot/js/edu.js
# Chạy ứng dụng ở 5211 trước:
node scripts/edu-e2e.mjs
```

Kiểm thử tạo email ngẫu nhiên `@example.test`, kiểm tra HTTP/SQL thực tế và dọn đúng các tài khoản QA trong `finally`. Không chạy vào database production. Lượt kiểm thử cuối: **27 kiểm tra đạt**, gồm 37 màn preview/auth, toàn bộ màn người học, các màn quản trị sau đăng nhập bằng mật khẩu, CSRF, quyền truy cập riêng, coin/reward, nội dung PDF riêng và đối soát ledger.

Giao diện đã được xem trên trình duyệt tại breakpoint mobile và desktop; các bản VIP chỉ được kiểm tra phần bố cục xem trước.

## Cập nhật giao diện 01/11/2026

- Phóng to khung đăng nhập, chữ, ô nhập và nút; bố cục thích ứng màn hình nhỏ.
- Nút sáng/tối và bộ chọn Tiếng Việt/English ở góc dưới phải, ghi nhớ trên trình duyệt. Nội dung tài liệu và câu hỏi nguồn giữ ngôn ngữ gốc.
- Quiz và nối khái niệm có báo cáo từng câu: lựa chọn, đáp án đúng do server trả sau khi chấm, giải thích, gợi ý ôn tập và liên kết nguồn/báo lỗi. Báo cáo giữ trong lượt làm bài hiện tại. Phân tích dùng lời giải có sẵn, chưa gọi AI trực tiếp.
- Ví coin chứa cả số dư, lịch sử, chính sách giá và các gói coin. `/edu/pricing` chuyển đến `/edu/wallet#coin-packages`; chọn gói chuyển đúng gói sang bước nạp.
- Kiểm chứng: build Release thành công, JavaScript hợp lệ, 27 kiểm tra trong `scripts/edu-e2e.mjs` qua; đã xem giao diện đăng nhập sáng/tối và kiểm tra đổi/nghi nhớ ngôn ngữ trên trình duyệt.
