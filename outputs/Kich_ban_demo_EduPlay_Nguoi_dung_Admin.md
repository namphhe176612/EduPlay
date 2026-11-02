# Kịch bản quay demo EduPlay — Người dùng và Admin

Thời lượng đề xuất: 10–12 phút. Chia video thành đúng hai phần: Người dùng (6–7 phút) và Admin (4–5 phút).

## Chuẩn bị trước khi quay

- Chạy SQL Server và `./scripts/start-edu.ps1`. Mở `http://localhost:5211/edu/login`.
- Dùng tài khoản demo, không dùng thông tin cá nhân thật. Chuẩn bị một tài khoản người học và một tài khoản SuperAdmin đã tạo mật khẩu. Có thể dùng hai trình duyệt để tránh nhầm phiên đăng nhập.
- Tài khoản người học chọn MKT101, Chương 4. Chuẩn bị một PDF có văn bản, khoảng 3–5 trang, đã thử tải lên và trích xuất được chữ. Không dùng PDF scan.
- Kiểm tra trước có flashcard, câu hỏi và case mẫu. Nếu quay phần duyệt nội dung, chuẩn bị sẵn nội dung trong hàng chờ Review; không trông chờ hàng chờ có dữ liệu tự nhiên.
- Đặt trình duyệt ở mức zoom 100%, tắt thông báo, đóng tab không liên quan. Khi chuyển màn hình, dừng khoảng 2 giây để người xem kịp theo dõi.
- Nhập mật khẩu  ngoài đoạn quay hoặc cắt đoạn nhập khi dựng video. 
- AI đang ở chế độ Demo: báo cáo từng câu dùng lời giải có sẵn; tạo bộ học từ PDF dùng trích đoạn nguồn. Thanh toán chưa cấu hình; VIP là bản xem trước. Lời thoại bên dưới đã phản ánh đúng trạng thái này.

## PHẦN 1 — NGƯỜI DÙNG (khoảng 6–7 phút)

### Cảnh 1 — Giới thiệu và đăng nhập (40 giây)

**Thao tác:** Mở `/edu/login`. Chỉ vào khung đăng nhập. Đổi sang giao diện sáng, chọn English; chuyển lại Tiếng Việt và giao diện tối. Đăng nhập bằng tài khoản demo đã chuẩn bị.

**Lời thoại:**

> Em xin chào thầy cô và các bạn. Đây là EduPlay, hệ thống hỗ trợ sinh viên ôn tập từ slide và tài liệu học tập. Video gồm hai phần: trải nghiệm của người học và chức năng quản trị. Đầu tiên là phần người học. Giao diện hỗ trợ chế độ sáng, tối và hai ngôn ngữ Anh, Việt. Lựa chọn được ghi nhớ trên trình duyệt; nội dung tài liệu và câu hỏi nguồn vẫn giữ ngôn ngữ gốc.

**Điểm dừng:** Hiển thị trang Hôm nay sau khi đăng nhập.

### Cảnh 2 — Môn học và trang tổng quan (35 giây)

**Thao tác:** Tại Hôm nay, chỉ vào môn/chương đang học. Mở Môn của tôi để giới thiệu MKT101 rồi quay về. Nếu cần thiết lập từ đầu, chọn MKT101, Chương 4 và lưu lịch học ở `/edu/onboarding` trước cảnh này.

**Lời thoại:**

> Sau khi đăng nhập, người học xem môn và chương đang theo học. Từ đây, em có thể mở slide, ôn flashcard, làm câu hỏi hoặc thực hành tình huống. Trong phần demo này, em sử dụng nội dung mẫu của môn MKT101, Chương 4 về chiến lược giá.

### Cảnh 3 — Đọc slide và ghi chú (40 giây)

**Thao tác:** Mở slide từ Công cụ. Chọn một trang, chỉ vào nội dung và khái niệm. Nhập ghi chú: “Phân biệt giá thâm nhập và giá hớt váng bằng mục tiêu thị trường.” Bấm Lưu ghi chú.

**Lời thoại:**

> Người học có thể đọc nội dung theo từng slide và ghi chú riêng. Em ghi lại điểm cần phân biệt giữa hai chiến lược giá, sau đó lưu ghi chú. Việc gắn nội dung ôn tập với slide nguồn giúp người học có căn cứ để kiểm tra lại kiến thức.

### Cảnh 4 — Ôn flashcard (35 giây)

**Thao tác:** Mở Flashcard. Lật một thẻ, chọn Chưa nhớ. Lật thẻ tiếp theo, chọn Nhớ rồi. Chỉ vào thống kê của phiên.

**Lời thoại:**

> Flashcard hỗ trợ ôn tập theo phương pháp lặp lại ngắt quãng. Với khái niệm chưa nhớ, em đánh dấu để gặp lại sớm hơn. Với khái niệm đã nhớ, hệ thống ghi nhận kết quả và lên lịch ôn tiếp theo. Đây là dữ liệu được lưu theo từng tài khoản.

### Cảnh 5 — Làm câu hỏi và xem báo cáo từng câu (75 giây)

**Thao tác:** Vào Tự kiểm tra. Đọc câu đầu, chọn một phương án. Dừng ở phần chấm và giải thích. Cuộn đến “Báo cáo AI · từng câu đã làm”, chỉ lần lượt lựa chọn, đáp án đúng, giải thích, gợi ý ôn tập và liên kết nguồn. Làm thêm một câu với kết quả khác nếu có thể. Mở lại báo cáo câu trước ngay trong lượt làm bài.

**Lời thoại:**

> Tiếp theo là chức năng tự kiểm tra. Khi chọn một phương án, máy chủ chấm câu trả lời và ghi nhận kết quả. Bên dưới là báo cáo cho từng câu đã làm, gồm lựa chọn của em, đáp án đúng, giải thích và gợi ý ôn lại. Em có thể xem lại các báo cáo trong lượt làm bài hiện tại và mở slide nguồn để đối chiếu. Trong phiên bản demo này, phần báo cáo dùng đáp án và lời giải có sẵn của bài học, chưa gọi AI trực tiếp.

**Nối sang phần Admin:** Bấm “Báo lỗi câu này” ở một câu vừa làm, chọn lý do phù hợp và nhập: “Góp ý demo: phần giải thích nên có thêm ví dụ phân biệt hai chiến lược giá.” Gửi một lần. Không khẳng định câu hỏi sai nếu chưa đối chiếu được.

**Lời thoại bổ sung:**

> Nếu thấy nội dung cần được kiểm tra hoặc làm rõ, người học có thể gửi phản hồi cho đội nội dung. Em gửi một góp ý minh họa để lát nữa xử lý trong phần quản trị. Mốc 48 giờ là thời hạn xử lý theo chính sách, không phải phản hồi tự động ngay lập tức.

### Cảnh 6 — Thực hành case và lưu kết quả (60 giây)

**Thao tác:** Mở case mẫu. Chỉ vào nhãn giả định. Chọn lần lượt đủ bốn cấp, dừng ngắn ở phản hồi. Mở kết quả, lưu vào ô case và chuyển sang Kho của tôi để xem kết quả đã lưu.

**Lời thoại:**

> Ngoài ghi nhớ và trả lời câu hỏi, EduPlay có tình huống thực hành theo bốn cấp: nhận diện, giải thích, liên hệ slide và vận dụng vào quyết định. Case đang trình diễn là tình huống giả định và được gắn nhãn rõ. Sau khi hoàn thành, em xem phản hồi và lưu kết quả vào kho. Hệ thống chỉ thưởng coin cho lần hoàn thành đầu tiên của mỗi case, tránh cộng thưởng lặp khi chơi lại.

**Lưu ý khi quay:** Nếu tài khoản đã hoàn thành case này trước đó, không nói “vừa được cộng thêm 1 coin”.

### Cảnh 7 — PDF riêng và tiến độ (45 giây)

**Thao tác:** Mở Tài liệu riêng, tải PDF đã chuẩn bị, mở nội dung. Nếu đã chạy thử ổn định, bấm Tạo bộ học và chỉ vào kết quả. Sau đó mở Tiến độ ôn tập để xem dữ liệu từ các câu vừa làm.

**Lời thoại:**

> Người học cũng có thể tải PDF riêng để đọc và tạo nội dung ôn tập. Tài liệu này được quản lý theo tài khoản, không đưa vào thư viện chung. Với chế độ Demo hiện tại, bộ học được tạo từ trích đoạn trong tài liệu, không gọi AI thật và không thu coin cho thao tác tạo bộ học. Phần tiến độ tổng hợp kết quả tự kiểm tra để người học biết những nội dung cần ôn thêm.

**Nếu xử lý PDF chậm:** Cắt đoạn chờ, hoặc nói: “Em mở tài liệu đã chuẩn bị trước để tiếp tục minh họa.” Không trình bày tài liệu chuẩn bị sẵn như kết quả vừa sinh xong.

### Cảnh 8 — Ví coin và bảng giá thống nhất (40 giây)

**Thao tác:** Mở “Ví & bảng giá coin”. Chỉ số dư, lịch sử, giá tính năng và các gói coin. Chọn một gói để mở trang nạp, dừng ở thông báo cổng thanh toán chưa cấu hình.

**Lời thoại:**

> Ví coin và bảng giá đã được gộp vào cùng một màn hình. Người học xem được số dư, lịch sử cộng trừ, chi phí tính năng và các gói coin. Khi chọn một gói, hệ thống chuyển đúng gói đó sang bước nạp. Cổng thanh toán hiện chưa được cấu hình, nên bản demo không thu tiền hoặc tự cộng coin từ thao tác này. Như vậy, phần người dùng đã đi từ đọc bài, ôn tập, thực hành đến theo dõi kết quả và quản lý coin.

## PHẦN 2 — ADMIN (khoảng 4–5 phút)

### Cảnh 1 — Đăng nhập quản trị và tổng quan (35 giây)

**Thao tác:** Chuyển trình duyệt hoặc đăng xuất người học. Mở `/edu/login`, đăng nhập tài khoản quản trị bằng email và mật khẩu. Sau đăng nhập, chỉ vào các thống kê và menu quản trị.

**Lời thoại:**

> Tiếp theo là phần quản trị. Khu vực này dùng chung trang đăng nhập với người học, hệ thống tự chuyển vào quản trị theo vai trò. Tài khoản người học không có quyền truy cập. Sau khi đăng nhập, quản trị viên xem số liệu tổng quan và các hàng đợi cần xử lý. Quyền thao tác được phân biệt theo vai trò biên tập, duyệt nội dung và SuperAdmin.

### Cảnh 2 — Xử lý phản hồi từ người học (65 giây)

**Thao tác:** Mở Báo lỗi tại `/edu/admin-reports`. Tìm đúng phản hồi có dòng “Góp ý demo” vừa gửi. Chỉ nội dung, lý do, trạng thái và hạn phản hồi. Đối chiếu nguồn trước khi quyết định. Với góp ý bổ sung ví dụ nhưng chưa có lỗi thực tế, nhập: “Đã đối chiếu nguồn: đáp án hiện tại phù hợp. Ghi nhận đề xuất bổ sung ví dụ.” rồi chọn Không phải lỗi.

**Lời thoại:**

> Đây là phản hồi em vừa gửi từ tài khoản người học. Quản trị viên xem nội dung, lý do và hạn phản hồi, sau đó đối chiếu với tài liệu nguồn. Với ví dụ này, nếu đáp án phù hợp và phản hồi chỉ đề nghị bổ sung ví dụ, em ghi nhận kết quả kiểm tra và đóng theo hướng không xác nhận lỗi. Người dùng không tự nhận coin chỉ bằng việc gửi báo lỗi. Khi một lỗi thực sự được xác nhận, hệ thống có cơ chế xử lý nội dung và thưởng 5 coin một lần cho người báo.

**Phương án nếu cần demo nhánh xác nhận lỗi:** Chuẩn bị riêng một nội dung thử nghiệm có lỗi đã biết. Chỉ xác nhận báo lỗi trên nội dung đó, sau khi hoàn tất phần quay người học. Việc xác nhận sẽ ẩn nội dung; không dùng câu mẫu chung đang cần cho các cảnh sau.

### Cảnh 3 — Duyệt và quản lý nội dung (45–60 giây)

**Thao tác:** Vào Duyệt nội dung tại `/edu/admin-review`. Nếu đã chuẩn bị nội dung Review, mở và chỉ các lựa chọn Duyệt/phát hành, Yêu cầu sửa, Từ chối; thực hiện quyết định phù hợp với nội dung thử nghiệm. Sau đó mở Thư viện slide và Thư viện case.

**Lời thoại:**

> Nội dung trước khi phát hành cần được kiểm tra với nguồn. Người có quyền duyệt có thể phát hành, yêu cầu chỉnh sửa hoặc từ chối. Thư viện slide quản lý các phiên bản tài liệu; thư viện case theo dõi tình huống và trạng thái nội dung. Case thực tế cần nguồn tham chiếu, còn tình huống giả định phải có nhãn để người học phân biệt.

**Nếu hàng chờ trống:** Nói “Hiện hàng chờ không có nội dung cần duyệt” và chuyển sang thư viện. Không nói vừa duyệt thành công khi chưa có thao tác thực tế.

### Cảnh 4 — Người dùng và điều chỉnh coin (55 giây)

**Thao tác:** Dùng SuperAdmin mở `/edu/admin-users`. Tìm đúng tài khoản demo. Chỉ tên, email, số dư và trạng thái. Nếu cần minh họa ghi sổ, cộng 2 coin với lý do “Điều chỉnh thử nghiệm trong video demo”. Chỉ thực hiện một lần. Chuyển về tài khoản người học, mở ví và chỉ giao dịch mới, rồi quay lại Admin.

**Lời thoại:**

> Với quyền SuperAdmin, em có thể quản lý trạng thái tài khoản và điều chỉnh coin kèm lý do. Ở đây em cộng thử 2 coin cho đúng tài khoản demo. Đây là điều chỉnh quản trị, không phải giao dịch nạp tiền. Khi mở lại ví của người học, số dư và lịch sử phản ánh thay đổi này. Việc ghi nhận từng giao dịch giúp kiểm tra nguồn gốc số dư.

**Tùy chọn:** Chỉ giới thiệu nút khóa/mở khóa. Nếu muốn thao tác, dùng tài khoản thử nghiệm thứ hai; không khóa tài khoản đang dùng để quay luồng chính.

### Cảnh 5 — Giá, tài chính và phân quyền (40 giây)

**Thao tác:** Lần lượt mở `/edu/admin-pricing`, `/edu/admin-finance`, `/edu/admin-roles`. Chỉ các trường dữ liệu và chức năng; không cần đổi giá hoặc vai trò chỉ để có thao tác.

**Lời thoại:**

> SuperAdmin còn có màn quản lý gói coin, xem chi phí AI và phân quyền nhân sự. Màn tài chính hiển thị chi phí từ các cuộc gọi AI thực tế đã ghi nhận; với chế độ Demo, không nên hiểu dữ liệu trống là AI đang được gọi miễn phí. Việc tách vai trò giúp phân biệt người biên tập, người duyệt và người quản lý toàn hệ thống.

### Cảnh 6 — Kết thúc (25 giây)

**Thao tác:** Quay về tổng quan Admin, dừng chuột và giữ màn hình vài giây.

**Lời thoại:**

> Qua hai phần vừa trình diễn, EduPlay kết nối quá trình học của sinh viên với việc quản lý chất lượng nội dung. Người học có thể đọc tài liệu, ôn tập, xem giải thích và gửi phản hồi; quản trị viên xử lý phản hồi, kiểm duyệt nội dung và quản lý tài khoản, coin. Phiên bản hiện tại đã có các luồng này; AI trực tiếp, thanh toán và các tính năng VIP là những phần cần tích hợp hoặc hoàn thiện tiếp. Em xin kết thúc phần demo.

## Ghi chú dựng video

- Chèn tiêu đề “PHẦN 1 — NGƯỜI DÙNG” và “PHẦN 2 — ADMIN” khi chuyển phần.
- Cắt thời gian nhập mật khẩu và chờ xử lý file. Giữ phần hiển thị kết quả sau mỗi thao tác.
- Báo cáo từng câu chỉ giữ trong lượt làm bài hiện tại; quay cảnh này trước khi rời trang hoặc tải lại.
- Không cần mở mọi màn hình. Ưu tiên mạch liền nhau: học → trả lời → xem báo cáo → gửi phản hồi → admin xử lý.
- Nếu cần rút còn 7–8 phút: bỏ cảnh PDF, rút flashcard còn một thẻ và chỉ giới thiệu ngắn màn giá/tài chính/phân quyền. Giữ cảnh báo cáo câu hỏi và phản hồi xuyên suốt hai vai trò.
