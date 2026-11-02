# Play edu — ASP.NET Core MVC

Ứng dụng học từ PDF bài giảng, dùng SQL Server **GameDB**.

## Chạy trên máy hiện tại

Yêu cầu: .NET SDK 10, SQL Server, Windows Authentication; SQL Server đang có GameDB với 11 bảng nền tảng.

Mở PowerShell tại thư mục dự án:

~~~powershell
dotnet restore PlayEdu.csproj
sqlcmd -S localhost -E -C -d GameDB -b -i Database/001_CoreExtensions.sql
dotnet run --project PlayEdu.csproj
~~~

Mở http://localhost:5210. Không mở trực tiếp file index.html; giao diện thật nằm trong Views và được MVC render.

Connection string trong appsettings.json dùng Windows Authentication. Có thể ghi đè bằng biến môi trường ConnectionStrings__GameDB. TrustServerCertificate=True chỉ phù hợp SQL Server phát triển cục bộ; production cần chứng chỉ hợp lệ và cấu hình riêng.

Database/000_Baseline_NewDatabase.sql là schema để dựng **database mới**, không chạy lên database đã có bảng. Máy hiện tại chỉ cần migration 001, đã được áp dụng.

## Tài khoản

- Đăng ký ở /Account/Register: role luôn bằng 1, được 20 coin và có ledger tương ứng.
- Ba tài khoản mẫu trong SQL ban đầu dùng DEMO_HASH, không có mật khẩu hợp lệ. Ứng dụng không tự đặt lại mật khẩu hoặc mở đăng nhập bằng placeholder.
- Đăng ký public không tạo được Admin.
- Tạo Admin riêng bằng script, mật khẩu được nhập ẩn và hash trước khi lưu:

~~~powershell
./scripts/create-admin.ps1 -Email "admin-cua-ban@example.com"
~~~

Script không ghi mật khẩu vào file hoặc tham số dòng lệnh. Email đã tồn tại sẽ bị từ chối, không ghi đè tài khoản.

## Những phần đã triển khai

| Module | Controller / service | Route UI | Lưu trữ |
|---|---|---|---|
| Đăng ký, đăng nhập, đăng xuất, role, tài khoản khóa | AccountController, cookie auth | /Account/Login, /Account/Register | Users, CoinTransactions |
| Tổng quan | HomeController | /Home/Dashboard | Dữ liệu thật theo UserId |
| PDF riêng tư, trích xuất chữ từng trang | DocumentsController, PdfPig | /Documents, /Documents/Upload, /Documents/Details/{id} | Files, Slides; PDF trong App_Data/documents |
| Dựng kịch bản, kiểm JSON/nguồn | ScenariosController, AiGateway, AiOperation | /Scenarios/Details/{id} | Scenarios, AICalls, CoinTransactions |
| Vào vai, chọn một lần, replay | ScenariosController, Simulation | /Scenarios/Play/{runId} | Runs, RunChoices |
| Kết quả, phương án khác, slide nguồn, đánh giá | ScenariosController | /Scenarios/Results/{runId} | Runs, RunChoices |
| Hỏi từ tài liệu, có slide nguồn | AiGateway, AiOperation | Trong trang Results | ScenarioMessages, AICalls |
| Luyện tập, đáp án chỉ chấm server | PracticeController | /Practice/Play/{id} | DeepPractices |
| Ví và ledger | WalletController, CoinService | /Wallet | CoinTransactions |
| Hồ sơ, tiếp tục lượt chơi | ProfileController | /Profile | Users, Runs |
| Lưu kết quả, 3 chỗ miễn phí | ProfileController, ScenariosController | /Profile/Vault | Saves |
| Admin: tìm user, coin, khóa/mở, usage/payment/audit | AdminController | /Admin | Users, CoinTransactions, AdminLogs, AICalls, Payments |
| Chơi thử public | HomeController + JavaScript | /Home/Sample | Không lưu; ghi rõ demo |
| Hướng dẫn, từ điển cơ bản, TTS trình duyệt | HomeController + JavaScript | /Home/Help; nút nghe ở Play | Không gọi AI |

## Demo và AI thật

Mặc định AI:Mode=Demo:
- Không gọi nhà cung cấp, không thu coin AI.
- Build dùng bốn tình huống theo mẫu chung với trích đoạn PDF. Chưa tạo tình huống Marketing riêng theo bối cảnh.
- Ask tìm trích đoạn liên quan, không giả vờ là câu trả lời AI.
- Luyện tập demo là câu hỏi nhận diện trích đoạn nguồn.
- Upload PDF 16–40 trang vẫn thu 15 coin khi thành công.

Để dùng provider hỗ trợ Chat Completions JSON mode, đặt cấu hình **trên server**, không đưa API key vào JavaScript hoặc Git:

~~~powershell
$env:AI__Mode = "Live"
$env:AI__Endpoint = "https://YOUR-PROVIDER/v1/chat/completions"
$env:AI__Provider = "Ten-provider"
$env:AI__Model = "model-do-ban-chon"
# Đặt AI__ApiKey qua secret manager hoặc biến môi trường cục bộ.
# AI__InputUsdPerMillion, AI__OutputUsdPerMillion: giá thực tế theo provider/model.
dotnet run --project PlayEdu.csproj
~~~

API key chưa được cung cấp, nên **chưa kiểm thử gọi provider thật**. Adapter hỗ trợ endpoint tương thích, không tuyên bố hỗ trợ API native của mọi hãng. Tham khảo định dạng API: https://developers.openai.com/api/reference/resources/chat/subresources/completions/methods/create

AiGateway validate cấu trúc, số scene, lựa chọn, metric, số slide và sourceQuote khớp nguyên văn. Kiểm tra này không chứng minh mọi suy luận của AI đều đúng. JSON sai ở Build được thử sửa tối đa một lần. Không chuyển sang demo âm thầm nếu Live lỗi.

## Coin và giao dịch

- Coin tính phía server. Client không chọn giá, số dư, role hoặc chất lượng đáp án.
- Đăng ký: +20 có ledger; không thưởng +5 khi hoàn thành.
- Build Live: 3; Ask: 3 câu đầu mỗi scenario miễn phí, sau đó 1/câu.
- Luyện tập Live: một lượt/ngày UTC tới 6 câu miễn phí; lượt khác làm tròn lên 1 coin/3 câu.
- PDF: 1–15 trang miễn phí; 16–40 trang cần đồng ý và trả 15 coin/tài liệu.
- Transaction giữ khóa theo user trong khi AI chạy. Coin, ledger, kết quả và log thành công commit cùng nhau; lỗi/hủy trước commit rollback toàn bộ và ghi call thất bại với CoinCharged=0. Không có khoản debit đã commit cần refund riêng trong thiết kế này.
- Giao dịch Admin sửa coin và audit commit cùng nhau. Không cho số dư âm.
- Chơi/replay không gọi AI.

## PDF và quyền riêng tư

- MIME application/pdf, đuôi .pdf, chữ ký %PDF-, dung lượng 20 MB, 1–40 trang.
- PdfPig trích chữ theo từng trang. Chưa có OCR.
- PDF gốc trong App_Data, không nằm trong wwwroot; SQL chỉ lưu đường dẫn/metadata.
- Xem PDF bằng viewer trình duyệt, chọn slide mở đúng trang và xem chữ đã trích. Chưa sinh thumbnail/ảnh raster phía server.
- Mọi file/scenario/run/practice có kiểm tra UserId trên server.
- Admin Role=2 không được vào controller tài liệu người học.
- Không log nội dung PDF, câu hỏi, API key hoặc mật khẩu.
- Data Protection keys lưu trong App_Data/keys, mã hóa DPAPI khi chạy Windows, thư mục này bị .gitignore.
- Production cần HTTPS, backup DB và App_Data, giới hạn tài nguyên xử lý PDF, quyền filesystem phù hợp, shared key/storage khi chạy nhiều instance.

## Điểm số

Hiện chấm quyết định /60 và chỉ số /20 bằng code, hiển thị **/80 điểm đã chấm**. Chưa có AI chấm lập luận /20 nên không cộng điểm giả để hiển thị /100. Catch-AI bonus chưa triển khai và không cộng vào điểm. Điểm cũ có sẵn trong seed giữ nguyên, không diễn giải như kết quả chấm mới.

## Thay đổi database

001_CoreExtensions.sql là migration idempotent:
- Saves: lưu kết quả, unique(UserId, RunId).
- ScenarioMessages: câu hỏi, câu trả lời, sourceSlide và chủ sở hữu.
- Giữ nguyên schema và seed của 11 bảng nền tảng.
- Không gọi EnsureDeleted/EnsureCreated hoặc tự seed đè dữ liệu.

## Kiểm thử

Khởi động ứng dụng ở http://localhost:5210, sau đó:

~~~powershell
node --check wwwroot/js/app.js
node scripts/e2e.mjs
~~~

Test dùng Node 22+ và sqlcmd, Windows Authentication. Chỉ chạy với database phát triển của dự án. Tạo email QA ngẫu nhiên, mật khẩu ngẫu nhiên trong bộ nhớ, thực hiện HTTP thật và kiểm tra SQL. finally chỉ xóa records/PDF thuộc email chính xác của lượt test, giữ nguyên dữ liệu sẵn có.

19 kiểm tra đã chạy (18 kiểm tra cơ bản + 1 kiểm tra Live cấu hình lỗi) bao gồm đăng ký chống giả role/coin, CSRF, upload/trích chữ/PDF riêng tư, unlock trang và rollback khi thiếu coin, demo build, khóa chọn lại, chơi hết/chấm điểm, hỏi nguồn, lưu idempotent, rating, luyện tập, ví/hồ sơ, Admin bị chặn PDF, ledger/audit, số dư âm, phiên bị khóa và rollback toàn bộ khi Live thiếu cấu hình provider.

## Phần chưa triển khai

Thanh toán thật/webhook, Google/Zalo OAuth, Prompt Lab, radar kỹ năng, AI tự đính chính/catch counter, chấm lập luận /20, unlock thêm slot kho, ảnh thumbnail PDF server-side, OCR, semantic dedup câu hỏi và kho câu sai được hỏi lại. Practice Live gửi lịch sử câu gần đây cho model nhưng chưa bảo đảm chống lặp bằng kiểm tra ngữ nghĩa.

Không có nút giả để thanh toán hay OAuth. Các gói coin chỉ là thông tin dự kiến; không thể tự xác nhận đã trả tiền.

## Cấu trúc

Controllers/ — HTTP, authorization và điều hướng
Models/ — entity và view model
Views/ — Razor MVC
Data/ — EF Core DbContext dùng GameDB
Services/ — coin, AI gateway/operation, tính điểm
Database/ — schema mới và migration không phá dữ liệu
wwwroot/ — CSS/JavaScript
App_Data/ — PDF và key riêng tư, không commit
scripts/ — tạo admin và kiểm thử


