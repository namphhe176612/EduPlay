(() => {
 "use strict";
 const read=(key,fallback)=>{try{return localStorage.getItem(key)||fallback;}catch{return fallback;}};
 let language=read('edu-language','vi');
 let theme=read('edu-theme','dark');
 if(!['vi','en'].includes(language))language='vi';
 if(!['light','dark'].includes(theme))theme='dark';
 const save=(key,value)=>{try{localStorage.setItem(key,value);}catch{}};
 document.documentElement.dataset.theme=theme;
 document.documentElement.lang=language;
 const dictionary=Object.fromEntries(`
Đăng nhập|Sign in
Đăng ký|Sign up
Tạo tài khoản|Create account
Đăng nhập quản trị|Admin sign in
Ôn tiếp bài trên lớp của bạn|Continue studying your coursework
Bắt đầu hành trình ôn tập của bạn.|Start your study journey.
Khu vận hành dành cho đội EduPlay|Workspace for the EduPlay team
Khám phá giao diện ↗|Explore EduPlay ↗
hoặc dùng email|or use email
Mật khẩu|Password
Nhập mật khẩu|Enter your password
Tối thiểu 8 ký tự|At least 8 characters
Nhập lại mật khẩu|Confirm password
Nhập lại|Enter again
Họ và tên|Full name
Họ tên|Full name
Ghi nhớ đăng nhập|Remember me
Quên mật khẩu?|Forgot password?
Chào bạn mới!|Welcome to EduPlay!
Chào mừng quay lại!|Welcome back!
TRỢ LÝ ÔN TẬP CHO SINH VIÊN|YOUR STUDENT STUDY ASSISTANT
Chưa có tài khoản? Đăng ký miễn phí để ôn đúng bài bạn đang học trên lớp, kèm 20 coin chào mừng.|New here? Sign up for free, study your coursework and receive 20 welcome coins.
Đã có tài khoản? Đăng nhập để ôn tiếp bài trên lớp và xem mình đã sẵn sàng cho kỳ thi chưa.|Already have an account? Sign in to continue learning and check your exam readiness.
Bước 1/2|Step 1/2
BẠN ĐANG HỌC|YOUR STUDIES
Trường|University
Kỳ hiện tại|Current semester
Bạn có học chuyên ngành Marketing không?|Are you majoring in Marketing?
Có, Marketing|Yes, Marketing
Ngành khác|Another major
Ngành khác vẫn đăng ký được. Nội dung đang mở thử cho Marketing.|All majors can register. The pilot content covers Marketing.
Bạn biết EduPlay qua đâu?|How did you hear about EduPlay?
Bạn bè|Friends
Giảng viên|Lecturer
Khác|Other
Mã giới thiệu (nếu có)|Referral code (optional)
Mã của bạn bè|Your friend's code
Nhận email nhắc học, môn mới và ưu đãi coin|Email me study reminders, new courses and coin offers
Tôi đồng ý Điều khoản sử dụng và Chính sách bảo mật|I agree to the Terms of Use and Privacy Policy
Tiếp tục: chọn môn →|Continue: choose courses →
Email công việc|Work email
Mã xác thực 6 số|6-digit verification code
Mở ứng dụng Authenticator trên điện thoại · mã đổi sau 30 giây|Open your Authenticator app · the code changes every 30 seconds
Vào khu quản trị|Open admin workspace
Về trang người học →|Back to student sign in →
Chỉ tài khoản được cấp quyền mới đăng nhập được. Sai 5 lần: khóa 15 phút.|Authorized accounts only. Five failed attempts lock access for 15 minutes.
Hiện mật khẩu|Show password
Ẩn mật khẩu|Hide password
Hôm nay|Today
Môn của tôi|My courses
Công cụ|Tools
Kho của tôi|My library
Tiến độ ôn tập|Study progress
Ví coin|Coin wallet
Ví & bảng giá coin|Wallet & coin pricing
Bảng giá coin|Coin pricing
Cài đặt|Settings
Đăng xuất|Sign out
Tài liệu riêng|Private documents
Lộ trình ôn thi|Exam study plan
Tổng quan|Overview
Duyệt nội dung|Content review
Báo lỗi|Reports
Báo lỗi nội dung|Content reports
Thiếu nội dung|Content gaps
Thư viện slide|Slide library
Thư viện case|Case library
Người dùng & coin|Users & coins
Bảng giá|Pricing
Tài chính & chi phí AI|Finance & AI costs
Phân quyền|Roles & permissions
Học từ slide của bạn|Learn from your slides
Tìm kiếm|Search
Bạn đang học môn gì?|What are you studying?
Đến nội dung chính|Skip to main content
Điều hướng chính|Main navigation
Tìm khái niệm|Find a concept
Tìm khái niệm trong slide của bạn…|Search concepts in your slides…
+ Nạp|+ Top up
BẢN XEM GIAO DIỆN · Dữ liệu học mẫu|INTERFACE PREVIEW · Sample learning content
Tạo tài khoản để lưu tiến độ →|Create an account to save progress →
Ôn đúng bài. Hiểu sâu hơn mỗi ngày.|Study your coursework. Understand more every day.
Khám phá công cụ ↗|Explore tools ↗
Thoát|Exit
Bắt đầu|Start
TỰ KIỂM TRA|QUIZ
TỰ KIỂM TRA · 10 PHÚT|QUIZ · 10 MINUTES
ÔN SAU BUỔI HỌC · 3 PHÚT|AFTER-CLASS REVIEW · 3 MINUTES
XEM SLIDE GỐC|SOURCE SLIDES
ÔN NHANH · NỐI KHÁI NIỆM|QUICK REVIEW · CONNECT CONCEPTS
ÁP DỤNG VÀO CASE · 20 PHÚT|CASE PRACTICE · 20 MINUTES
Mình đã hiểu Chương 4 chưa?|How well do I understand Chapter 4?
Nối các khái niệm Chương 4|Connect Chapter 4 concepts
Đang chuẩn bị câu hỏi…|Preparing your questions…
Tiến độ phiên|Session progress
Câu hỏi lấy từ đâu?|Where do these questions come from?
Nội dung mẫu theo khái niệm của bài học. Mỗi câu có giải thích và liên kết slide nguồn.|Sample content based on course concepts. Each question includes an explanation and a source slide.
Tình huống vận dụng nằm ở phần|Find practical scenarios in
Câu tiếp →|Next question →
▣ Xem slide nguồn|▣ View source slide
⚑ Báo lỗi câu này|⚑ Report this question
Bạn đã hoàn thành!|Session complete!
Xem tiến độ →|View progress →
Báo cáo AI · từng câu đã làm|AI report · answered questions
Phân tích theo đáp án và giải thích có sẵn từ bài học; chưa gọi AI trực tiếp.|Analysis uses the course answer key and existing explanations; no live AI call has been made.
Câu hỏi|Question
Bạn đã chọn|Your answer
Đáp án đúng|Correct answer
Giải thích|Explanation
Gợi ý ôn tập|Study suggestion
Bạn đã nắm đúng ý chính. Thử giải thích lại bằng lời của mình.|You understood the main idea. Try explaining it in your own words.
Đọc lại slide nguồn, so sánh lựa chọn của bạn với đáp án rồi thử lại.|Review the source slide, compare your choice with the correct answer, then try again.
CHÍNH XÁC · VÌ SAO|CORRECT · HERE IS WHY
CHƯA ĐÚNG · CÙNG XEM LẠI|INCORRECT · LET'S REVIEW
Đúng|Correct
Cần ôn|Needs review
Xem slide nguồn →|View source slide →
Đăng nhập để sử dụng và lưu tiến độ học tập.|Sign in to use this feature and save progress.
Đăng nhập để gửi báo lỗi.|Sign in to report an issue.
Đăng nhập để làm bài và lưu kết quả.|Sign in to answer questions and save results.
Không thể lưu thao tác. Hãy kiểm tra kết nối hoặc đăng nhập lại.|Unable to save. Check your connection or sign in again.
Không tải được câu hỏi. Thử tải lại trang.|Unable to load the question. Reload the page to try again.
Chưa tải được câu hỏi.|Unable to load the question.
SỐ DƯ|BALANCE
Coin mua lẻ, dùng bao nhiêu trả bấy nhiêu.|Buy coins as needed and pay only for what you use.
+ Nạp coin|+ Top up coins
Nạp coin|Top up coins
Lượt miễn phí|Free allowance
Làm mới 00:00|Resets at midnight
Hạn mức được áp dụng khi tính năng AI được cấu hình.|Allowances apply when AI features are configured.
Nhận thêm coin|Earn more coins
Quà chào mừng|Welcome gift
Khi tạo tài khoản|When you create an account
Hoàn thành một case lần đầu|Complete a case for the first time
Mỗi case chỉ thưởng một lần|Each case rewards coins only once
Giữ chuỗi 7 ngày|Maintain a 7-day streak
Học liên tục mỗi ngày|Study every day
Báo lỗi được xác nhận|Confirmed content report
Đội nội dung xác nhận đúng là lỗi|The content team confirms the issue
Mời bạn cùng lớp|Invite classmates
Sau buổi học đầu tiên của bạn bè|After your friend's first study session
Một số chương trình thưởng cần được kích hoạt sau khi hoàn thiện điều kiện xác minh.|Some reward programs require activation after verification is implemented.
Lịch sử coin|Coin history
Chưa có giao dịch.|No transactions yet.
Coin dùng vào đâu|How coins are used
Chất vấn AI (sau 3 lượt/case)|Ask AI (after 3 requests per case)
Gợi ý slide|Slide hints
Tra từ AI (sau 10 lượt/ngày)|AI glossary (after 10 requests per day)
Học sâu (sau 6 câu/ngày)|Deep study (after 6 questions per day)
Dựng case từ slide|Create a case from slides
Mở tài liệu đến 40 trang|Unlock up to 40 document pages
Thêm 1 ô lưu|Add a save slot
Học theo slide và chơi case có sẵn luôn miễn phí.|Studying slides and playing existing cases is always free.
Gói coin & bảng giá|Coin packages & pricing
Học theo slide luôn miễn phí. Coin chỉ dùng cho các tính năng mở rộng.|Studying slides is always free. Coins are only used for additional features.
Phổ biến|Popular
Gói coin|Coin package
Chọn gói →|Select package →
Gói lớn hơn thì mỗi coin rẻ hơn. Không gia hạn, không tự động trừ tiền.|Larger packages cost less per coin. No renewals or automatic charges.
Gói coin linh hoạt|Flexible coin packages
1 · Chọn gói coin|1 · Choose a coin package
2 · Phương thức thanh toán|2 · Payment method
Đơn của bạn|Your order
Gói đã chọn|Selected package
Số dư sau khi nạp|Balance after top-up
Tổng thanh toán|Total payment
Kiểm tra thanh toán|Check payment availability
Cổng thanh toán chưa được cấu hình. Nút bên dưới kiểm tra trạng thái, chưa thực hiện thu tiền.|Payments are not configured. The button checks availability and does not charge you.
Coin chỉ vào ví khi máy chủ xác minh giao dịch thành công.|Coins are credited only after the server verifies a successful payment.
♛ VIP · sắp có|♛ VIP · coming soon
EduPlay VIP sắp ra mắt|EduPlay VIP is coming soon
Xem trước →|Preview →
Nhắc ôn|Study reminders
Nhắc mỗi ngày|Daily reminder
Nhắc sau buổi học|After-class reminder
Nhắc trước kỳ thi|Before-exam reminder
Báo khi giảng viên cập nhật slide|Notify me when slides are updated
Thời gian nhắc|Reminder time
Giờ Việt Nam · UTC+7|Vietnam time · UTC+7
Lịch học|Study schedule
Môn đang học và lịch thi|My courses and exam schedule
Sửa →|Edit →
Xem chương theo tuần|View chapters by week
Lưu cài đặt|Save settings
Tài khoản|Account
Mã giới thiệu|Referral code
Sao chép|Copy
Quyền riêng tư|Privacy
EduPlay ghi nhớ về bạn|Let EduPlay remember your preferences
Dùng để gợi ý phần cần ôn|Used to suggest topics to review
Tài khoản đang hoạt động|Active accounts
Nội dung chờ duyệt|Content awaiting review
Báo lỗi đang mở|Open reports
Nội dung cần bổ sung|Content gaps
⚑ Báo lỗi câu hỏi|⚑ Report a question
Đóng|Close
Đội nội dung sẽ đối chiếu với slide nguồn.|The content team will check the source slides.
Đáp án đúng bị chấm sai|A correct answer was marked wrong
Giải thích không khớp với slide|The explanation does not match the slides
Thiếu nguồn hoặc sai số slide|Missing source or incorrect slide number
Câu hỏi khó hiểu / lỗi chính tả|Unclear question or typo
Lý do khác|Another reason
Mô tả thêm (không bắt buộc)|Additional details (optional)
Báo đúng lỗi: +5 coin sau xác nhận|Confirmed issue: +5 coins after review
Gửi báo lỗi|Submit report
Kiểm tra trong 48 giờ. Coin chỉ được thưởng khi lỗi được xác nhận.|Reviewed within 48 hours. Coins are awarded only for confirmed issues.
Đổi giao diện|Switch theme
Ngôn ngữ|Language
Giao diện và ngôn ngữ|Appearance and language
`.trim().split('\n').map(line=>line.split('|')));
 Object.assign(dictionary,Object.fromEntries("\nBạn chưa chọn môn nào|You have not selected any courses\nChọn môn đang học trên lớp để EduPlay đưa đúng slide và lịch ôn.|Choose your current courses to see the right slides and study schedule.\nChọn môn →|Choose courses →\nTRÊN LỚP HÔM NAY · ÔN ĐÚNG BÀI CỦA BẠN|TODAY'S CLASS · REVIEW YOUR COURSEWORK\n5 khái niệm · ôn từ slide · khoảng 3 phút|5 concepts · review from slides · about 3 minutes\nBắt đầu ôn · 3 phút →|Start reviewing · 3 minutes →\nCòn sức?|Keep going?\nTự kiểm tra 10 phút|10-minute quiz\nCase 20 phút|20-minute case\nKhái niệm của chương|Chapter concepts\nChưa ôn|Not reviewed\nĐã vững|Mastered\nChưa có nội dung cho chương này. Bạn có thể thêm slide riêng.|No content for this chapter yet. You can add your own slides.\nCHUẨN BỊ CHO KỲ THI|PREPARE FOR YOUR EXAMS\nHọc chắc hơn từng chương|Build confidence chapter by chapter\nLưu lịch thi và theo dõi mức sẵn sàng từ kết quả ôn tập.|Save your exam dates and track readiness from your study results.\nXem tiến độ ôn tập →|View study progress →\nTrên lớp có chỗ chưa hiểu?|Need help understanding your class?\nTìm|Search\nTìm khái niệm và quay về slide nguồn của bài học.|Find concepts and return to the source slides.\n☼ Xem tất cả công cụ: ôn tập, case, lộ trình… →|☼ Explore all tools: reviews, cases, study plans… →\nMỗi lần ôn, hiểu thêm một chút.|Understand a little more with every review.\nChọn môn và lịch học để EduPlay đồng hành đúng nhịp lớp của bạn.|Choose your courses and schedule to keep EduPlay in step with your classes.\nChọn môn học →|Choose courses →\nBạn đang cần gì?|What would you like to do?\n▤ Mình chưa hiểu bài|▤ Help me understand\nϟ Mình muốn luyện|ϟ I want to practice\n◎ Áp dụng case thật|◎ Apply concepts to cases\n▣ Tài liệu của mình|▣ My documents\nHọc theo slide luôn miễn phí, chỉ trả coin khi dùng AI.|Learning from slides is free. Coins apply only to AI features.\nBảng giá coin →|Coin pricing →\nMở →|Open →\n+ Thêm môn|+ Add course\n▣ Theo slide giảng viên|▣ Based on your lecturer's slides\nCác chương xếp theo tuần học trên lớp. Ôn và áp dụng bài đã học.|Chapters follow your class schedule. Review and apply what you have learned.\nÔn lại|Review\nTự kiểm tra|Quiz\nThư viện môn này đang chuẩn bị|This course library is being prepared\nTải slide riêng để lưu và đọc bài của bạn.|Upload your own slides to save and read your coursework.\nThêm tài liệu|Add documents\nLịch của môn|Course schedule\nBuổi học và ngày thi|Classes and exam dates\nCập nhật theo lịch học của bạn.|Updated from your study schedule.\nSửa lịch học →|Edit study schedule →\nTài liệu bạn thêm cho môn này|Your documents for this course\nSlide và ghi chú riêng chỉ mình bạn xem.|Only you can see your private slides and notes.\n+ Thêm slide / ghi chú|+ Add slides / notes\nBước 2/2|Step 2/2\nChọn môn và lịch học để ôn đúng bài trên lớp.|Choose courses and a schedule to review your coursework.\n1 · Chọn môn có sẵn trong thư viện|1 · Choose courses from the library\n2 · Môn chưa có? Thả slide của bạn vào đây|2 · Course unavailable? Add your own slides\nKéo thả file PDF hoặc chọn từ máy|Drop a PDF or choose one from your computer\nThêm tài liệu riêng ở Kho của tôi · tối đa 20 MB, 40 trang|Add private documents to My library · up to 20 MB, 40 pages\nFile riêng không đưa vào thư viện chung. 15 trang đầu miễn phí.|Private files are not shared in the public library. The first 15 pages are free.\n3 · Lịch học của bạn|3 · Your study schedule\nLớp đang học tới|Current chapter\nNgày thi giữa kỳ|Midterm date\nBuổi học trong tuần|Weekly classes\n← Quay lại|← Back\nBắt đầu học →|Start studying →\nBạn sẽ nhận được:|You will receive:\nÔn đúng chương lớp đang học|Review the chapter your class is studying\nNhớ lâu hơn bằng flashcard và tự kiểm tra|Remember more with flashcards and quizzes\n20 coin chào mừng|20 welcome coins\n← Về chương|← Back to chapter\nTô sáng|Highlight\n▤ Ghi chú|▤ Notes\nKhái niệm trên slide này|Concepts on this slide\nThuật ngữ tiếng Anh kèm nghĩa tiếng Việt|English terms with Vietnamese definitions\n+ Ôn bằng flashcard →|+ Review with flashcards →\n▤ Ghi chú của bạn|▤ Your notes\nLưu ghi chú|Save notes\nChỉ mình bạn xem. Ghi chú giúp bạn ôn bài sau này.|Visible only to you. Notes help you review later.\nĐang dùng slide này ở|This slide is used in\nFlashcard Chương 4|Chapter 4 flashcards\nTự kiểm tra Chương 4|Chapter 4 quiz\nKHÁI NIỆM · PRICING|CONCEPT · PRICING\nBấm vào thẻ hoặc nhấn Space để lật|Click the card or press Space to flip\n← Chưa nhớ|← Not yet\nNhớ rồi →|Remembered →\ngặp lại sau 10 phút|review again in 10 minutes\nlên lịch ôn lần sau|schedule the next review\nĐã hoàn thành lượt ôn.|Review complete.\nVề Hôm nay →|Back to Today →\nMẶT SAU KHI LẬT|REVERSE SIDE\nLật thẻ để xem định nghĩa và tự kiểm tra trí nhớ.|Flip the card to see the definition and check your memory.\n▣ MKT101 · Ch.4 · slide nguồn|▣ MKT101 · Ch.4 · source slide\nPhiên này|This session\nNhớ rồi|Remembered\nÔn lại sau|Review later\nÔn lặp lại ngắt quãng: thẻ nhớ chắc được lên lịch xa dần (1 → 3 → 7 → 14 ngày).|Spaced repetition: remembered cards are scheduled at longer intervals (1 → 3 → 7 → 14 days).\nLiên kết các khái niệm từ slide|Connect concepts from your slides\n■ Đúng|■ Correct\n■ Sai · sẽ hỏi lại|■ Incorrect · review again\nBỐI CẢNH|CONTEXT\nXem kết quả →|View results →\nSlide gợi ý|Suggested slides\nVận dụng 4 cấp|4 levels of practice\nNhận diện khái niệm|Identify concepts\nGiải thích vì sao|Explain why\nLiên hệ với slide|Connect to slides\nVận dụng vào quyết định|Apply to decisions\nHỏi AI|Ask AI\nAI chưa được cấu hình. Bạn vẫn có thể học từ slide nguồn miễn phí.|AI is not configured. You can still study the source slides for free.\nĐÃ VẬN DỤNG KIẾN THỨC|KNOWLEDGE APPLIED\nKết quả case của bạn|Your case results\nBạn đã chọn gì|Your choices\nHoàn thành case để xem lại lựa chọn của mình.|Complete a case to review your choices.\nPhần thưởng|Rewards\nThẻ case đầu tiên|First case card\nThưởng một lần cho mỗi case hoàn thành.|One reward for each completed case.\nKết quả thật được lưu trong Kho của tôi.|Your results are saved in My library.\nLưu kết quả vào ô case|Save results in a case slot\nÔn lại khái niệm →|Review concepts →\nXem kho của tôi|View my library\nVề Hôm nay|Back to Today\nTất cả|All\nTài liệu riêng →|Private documents →\nLỊCH SỬ HỌC TẬP|STUDY HISTORY\nKho của bạn bắt đầu từ đây|Your library starts here\nMọi lần ôn, câu trả lời và case đã học sẽ được lưu ở đây.|Your reviews, answers and completed cases will be saved here.\nÔn bài đầu tiên →|Start your first review →\nCâu sai cần làm lại|Questions to retry\ncâu|questions\nLàm lại câu hỏi →|Retry questions →\nÔ lưu case|Case save slots\n3 ô miễn phí · tối đa 9 ô|3 free slots · up to 9 slots\nTài liệu PDF riêng được quản lý độc lập với ô lưu kết quả case.|Private PDFs are managed separately from case result slots.\nThêm ô · 10 coin|Add slot · 10 coins\nXem ví coin →|View coin wallet →\nTiến độ từ các lần tự kiểm tra giúp bạn biết phần cần ôn thêm.|Quiz progress helps you identify topics to review.\nQuản lý quyền riêng tư →|Manage privacy →\n1 · Tải PDF|1 · Upload PDF\nSlide hoặc ghi chú của bạn|Your slides or notes\n2 · Đọc từng trang|2 · Read each page\nTrích xuất chữ từ PDF|Extract text from PDF\n3 · Học theo nguồn|3 · Learn from sources\nĐọc và ghi chú riêng|Read and take private notes\nKéo thả file PDF vào đây|Drop your PDF here\nChỉ nhận PDF, tối đa 20 MB · không quá 40 trang|PDF only, up to 20 MB · maximum 40 pages\nChọn file từ máy|Choose a file\nTải tài liệu lên →|Upload document →\nFile của bạn|Your files\nMiễn phí đến 15 trang mỗi tài liệu|First 15 pages of each document are free\nMở PDF|Open PDF\nTạo bộ học|Create study set\nMở đủ trang · 15 coin|Unlock all pages · 15 coins\nXóa|Delete\nChưa có tài liệu riêng. Tải PDF đầu tiên để bắt đầu.|No private documents yet. Upload your first PDF to get started.\n♙ RIÊNG TƯ TUYỆT ĐỐI|♙ PRIVATE TO YOU\nChỉ bạn xem được file này. File không vào thư viện chung và không ai khác truy cập được.|Only you can view this file. It is not added to the shared library and others cannot access it.\nBạn có thể xóa tài liệu khỏi kho bất kỳ lúc nào.|You can remove documents from your library at any time.\nChi phí|Costs\nNội dung trong thư viện: miễn phí.|Library content is free.\nSẵn sàng cho kỳ thi MKT101|MKT101 exam readiness\nTính từ kết quả tự kiểm tra. Bắt đầu ôn để có dữ liệu thực tế.|Based on quiz results. Start reviewing to build your progress data.\nLộ trình ôn thi · VIP sắp có →|Exam study plan · VIP coming soon →\nNên ôn trước khi thi|Review before the exam\nThành tích nhỏ|Small achievements\nMỗi ngày một bước|One step every day\nHoàn thành một phiên ôn để bắt đầu chuỗi.|Complete a review to start your streak.\nBộ sưu tập case|Case collection\nVận dụng lý thuyết vào quyết định.|Apply theory to decisions.\nHiểu bài lâu hơn|Remember for longer\nQuay lại ôn đúng lúc bằng flashcard.|Return at the right time with flashcard reviews.\nVideo 90 giây, bộ ôn theo đề cương, thi thử…|90-second videos, exam study sets, mock exams…\nTính năng|Feature\nMiễn phí|Free\nGiá|Price\nSắp có|Coming soon\nChương|Chapter\nTên|Name\nPhiên bản|Version\nTrạng thái|Status\nMã|ID\nNội dung|Content\nLý do|Reason\nĐiểm|Score\nDoanh nghiệp|Company\nLoại|Type\nVai trò|Role\nSố thành viên|Members\nLưu giá|Save pricing\nLưu|Save\n".trim().split('\n').map(line=>line.split('|'))));
 Object.assign(dictionary,{"Đăng nhập Google":"Sign in with Google","Đăng nhập Facebook":"Sign in with Facebook","Đăng nhập Microsoft":"Sign in with Microsoft","Google OAuth chưa được cấu hình.":"Google sign-in is not configured.","Facebook OAuth chưa được cấu hình.":"Facebook sign-in is not configured.","Đăng nhập Microsoft chưa được cấu hình.":"Microsoft sign-in is not configured.","Đặt lại mật khẩu qua email cần cấu hình máy chủ gửi thư. Liên hệ quản trị viên để được hỗ trợ.":"Email password reset requires a mail server. Contact an administrator for help."});
 Object.assign(dictionary,{"Chất vấn AI":"Ask AI","Học sâu":"Deep study","Tra từ AI":"AI glossary","Trợ lý hướng dẫn":"Study assistant","Mở PDF đến 40 trang":"Unlock up to 40 PDF pages","Thêm ô lưu":"Add save slots","/ ngày":"/ day","1 coin/3 câu":"1 coin/3 questions"});
 Object.assign(dictionary,Object.fromEntries("\nHiểu bài|Understand\nTrên lớp chưa rõ chỗ nào thì xem lại ở đây|Review anything you did not understand in class\nXem slide gốc|View source slides\nSlide giảng viên, tô sáng khái niệm, ghi chú riêng|Lecturer slides, highlighted concepts and private notes\nTìm khái niệm và quay lại nguồn trong bài học|Find concepts and return to their course sources\nTừ điển + trợ từ AI|Glossary & AI assistance\nVideo 90 giây|90-second video\nAI giảng lại một khái niệm, có nguồn|AI explains a concept with source references\nVIP · sắp có|VIP · coming soon\nLuyện tập|Practice\nÔn ngắn mỗi ngày để nhớ lâu|Short daily reviews for lasting knowledge\nÔn lặp lại|Spaced repetition\nThẻ đến hạn hôm nay, ôn đúng lúc sắp quên|Review cards due today, just before you forget\nCâu hỏi từ bài học, có giải thích và số slide|Course questions with explanations and slide numbers\nNối khái niệm|Connect concepts\nNhìn nhanh sự liên quan giữa các khái niệm|See how concepts relate at a glance\nLuyện câu hỏi và quay lại phần còn yếu|Practice questions and revisit weaker topics\nCase thật|Real-world cases\nÁp dụng lý thuyết vào tình huống doanh nghiệp|Apply theory to business scenarios\nChơi case có sẵn|Play existing cases\nNhận diện, giải thích, liên hệ slide và vận dụng|Identify, explain, connect to slides and apply\nTải tài liệu riêng để chuẩn bị nội dung học|Upload private documents to prepare study material\nHỏi sâu về tình huống, bám nguồn bài học|Explore scenarios using course sources\nChuẩn bị thi|Exam preparation\nGói mở rộng EduPlay VIP · mở sau giai đoạn pilot|EduPlay VIP extras · available after the pilot\nLịch ôn mỗi ngày, thích ứng với tiến độ của bạn|A daily study plan adapted to your progress\nThi thử|Mock exam\nLàm bài theo cấu trúc giảng viên công bố|Practice with your lecturer's exam structure\nBộ ôn theo đề cương|Syllabus study pack\nĐối chiếu đề cương với các chương slide|Match your syllabus to slide chapters\nXuất đề cương PDF|Export study outline PDF\nGom trọng tâm, câu sai và ghi chú thành file|Combine key topics, mistakes and notes in one file\nDùng ngay|Available now\n10 lượt/ngày miễn phí|10 free requests/day\n6 câu/ngày miễn phí|6 free questions/day\n3 lượt/case miễn phí|3 free requests/case\n3 coin · khi AI được cấu hình|3 coins · when AI is configured\nMiễn phí hoàn toàn|Completely free\nĐăng ký và đọc slide mẫu|Register and read sample slides\n15 trang PDF riêng đầu tiên|First 15 private PDF pages\nFlashcard, ôn lặp lại, tự kiểm tra|Flashcards, spaced repetition and quizzes\nChơi lại case và xem giải thích|Replay cases and view explanations\nGhi chú riêng và liên kết slide nguồn|Private notes and source slide links\nTiến độ ôn tập, ví coin và lịch sử|Study progress, coin wallet and history\nMiễn phí có hạn mức|Free with allowances\nSau hạn mức|After the allowance\nTính năng AI cần cấu hình nhà cung cấp.|AI features require a configured provider.\nTính bằng coin|Paid with coins\nMiễn phí kèm theo|Included for free\nChơi lại không tốn coin|Replay for free\nĐọc slide gốc miễn phí|Read source slides for free\n15 trang đầu miễn phí|First 15 pages free\nThêm 1 ô lưu (tối đa 9)|Add 1 save slot (up to 9)\n3 ô đầu miễn phí|First 3 slots free\nEduPlay VIP · gói mở rộng cho mùa thi|EduPlay VIP · exam season extras\nMở sau giai đoạn pilot. Giá được công bố khi ra mắt.|Available after the pilot. Pricing announced at launch.\nXem trước tính năng →|Preview features →\nVideo 90 giây giảng lại bài|90-second lesson videos\nThi thử theo cấu trúc đề|Mock exams matching your exam structure\nLộ trình ôn và xuất PDF|Study plans and PDF exports\nEduPlay không lưu thông tin thẻ. Thanh toán được xử lý qua cổng thanh toán khi được cấu hình.|EduPlay does not store card details. Payments use a payment gateway when configured.\nCài đặt được lưu. Gửi email và thông báo trình duyệt cần cấu hình dịch vụ gửi.|Settings are saved. Email and browser notifications require a configured delivery service.\nTài liệu riêng chỉ tài khoản của bạn có quyền truy cập.|Only your account can access your private documents.\nNhắc đúng thời gian bạn muốn học|Remind me when I want to study\nDựa trên lịch học bạn đã nhập|Based on your class schedule\nChuẩn bị cho kỳ thi của bạn|Prepare for your exams\nNhận thông báo khi môn có bản mới|Notify me when a course has new slides\nPenetration Pricing trong 90 giây|Penetration Pricing in 90 seconds\nPhụ đề tiếng Việt · nội dung từ slide nguồn|Vietnamese subtitles · content from source slides\nKịch bản 6 cảnh|6-scene storyboard\nMỗi cảnh gắn nguồn bài học|Every scene references a course source\nTạo video mới|Create a video\nMôn · chương|Course · chapter\nKhái niệm cần giảng|Concept to explain\nPhong cách|Style\nGiảng viên hoạt hình|Animated lecturer\nSlide + giọng đọc|Slides + narration\nBảng trắng vẽ tay|Hand-drawn whiteboard\nGiọng đọc|Narration voice\nTiếng Việt · nữ|Vietnamese · female\nTiếng Việt · nam|Vietnamese · male\nChỉ là bản xem trước. Giá công bố khi ra mắt.|Preview only. Pricing announced at launch.\nLỘ TRÌNH ÔN THI · BẢN XEM TRƯỚC|EXAM STUDY PLAN · PREVIEW\nMỗi ngày một chút, sẵn sàng hơn khi thi|A little every day, more ready for your exam\nKhoảng 20 phút mỗi ngày · nghỉ Chủ nhật · điều chỉnh theo kết quả|About 20 minutes daily · Sundays off · adapted to results\nNghỉ · nạp lại năng lượng|Rest · recharge\nÔn flashcard|Review flashcards\nĐiều chỉnh|Adjustments\nThời gian ôn mỗi ngày|Daily study time\n20 phút|20 minutes\n30 phút|30 minutes\nNgày nghỉ|Rest day\nChủ nhật|Sunday\nNhắc ôn lúc|Remind me at\nLịch minh họa, chưa áp dụng cho tài khoản của bạn.|Illustrative schedule, not applied to your account.\nTự điều chỉnh theo tiến độ|Adapts to your progress\nƯu tiên phần còn yếu từ kết quả tự kiểm tra và ngày thi bạn đã nhập.|Prioritizes weaker topics using quiz results and your exam dates.\n⚑ Đánh dấu xem lại|⚑ Flag for review\nBản xem trước. Khi thi sẽ không hiện đáp án; giải thích chỉ hiện sau khi nộp bài.|Preview. Answers are hidden during exams; explanations appear after submission.\nBảng câu hỏi|Question navigator\nCấu trúc đề minh họa|Example exam structure\nTrắc nghiệm|Multiple choice\nTự luận ngắn|Short answer\nTình huống|Scenarios\nĐề ôn theo slide. Không phải đề thi thật.|Practice based on slides. This is not an actual exam.\nTrọng tâm theo đề cương|Key syllabus topics\nCâu không tìm thấy trong slide sẽ được đánh dấu. EduPlay không tự bịa đáp án.|Questions without slide support are flagged. EduPlay does not invent answers.\nBộ ôn theo đúng dạng đề|Study sets matching exam formats\nTạo bộ ôn mới|Create a study set\nThả đề cương / bộ câu hỏi ôn tập|Drop a syllabus / review question set\nPDF hoặc ảnh chụp · tính năng sắp có|PDF or photo · coming soon\nMôn|Course\nNgày thi|Exam date\nChỉ dùng đề cương được phép chia sẻ. Không đoán đề thi thật.|Use syllabuses you are allowed to share. No predictions of actual exam questions.\nNội dung đưa vào|Include content\nMôn · kỳ thi|Course · exam\nKhổ giấy|Paper size\nA4 · in hai mặt|A4 · double-sided\nXuất PDF thuộc gói VIP sắp có.|PDF export is part of the upcoming VIP plan.\nTRẠNG THÁI|STATES\nNhững lúc người dùng cần được dẫn đường|Guidance when you need it\nChưa tìm thấy khái niệm|No concepts found\nMở slide bài học|Open course slides\nBản xem bố cục quản trị. Dữ liệu vận hành chỉ hiển thị sau đăng nhập và kiểm tra quyền.|Admin layout preview. Operational data requires sign-in and permission checks.\nTheo dõi từ dữ liệu thực tế|Tracked from actual data\nViệc cần làm|To-do list\nChưa có việc đang mở|No open tasks\nHàng đợi sẽ cập nhật khi có báo lỗi hoặc nội dung thiếu.|The queue updates when reports or content gaps are submitted.\nMở hàng đợi nội dung →|Open content queue →\nChi phí AI theo tính năng|AI costs by feature\nChưa có cuộc gọi AI trong database mới. Chi phí sẽ được ghi theo từng lần gọi thật.|No AI calls in the new database yet. Costs are recorded for actual calls.\nXem tài chính và chi phí →|View finance and costs →\nNội dung chờ kiểm duyệt|Content awaiting review\nDuyệt · phát hành|Approve · publish\nYêu cầu sửa|Request changes\nTừ chối|Reject\nKhông có nội dung chờ duyệt|No content awaiting review\nNội dung mới cần được đối chiếu nguồn trước khi phát hành.|New content must be verified against sources before publication.\nNgười duyệt kiểm tra|Reviewer checklist\nSố liệu khớp nguồn bài báo|Figures match the source article\nKhái niệm khớp slide|Concepts match the slides\nNguồn nằm trong danh sách cho phép|Sources are on the allowed list\nĐáp án có giải thích vì sao|Answers include explanations\nNgôn ngữ phù hợp sinh viên|Language is suitable for students\nTình huống giả định có nhãn rõ|Hypothetical scenarios are clearly labeled\nCam kết phản hồi người học trong 48 giờ|Commitment to respond within 48 hours\nHạn phản hồi|Response deadline\nXác nhận lỗi · +5 coin|Confirm issue · +5 coins\nKhông phải lỗi|Not an issue\nChưa có báo lỗi|No reports yet\nBáo lỗi của người học sẽ được đưa vào hàng đợi này.|Student reports appear in this queue.\nDuyệt phiên bản|Approve version\nTải slide giảng viên (PDF)|Upload lecturer slides (PDF)\nTải phiên bản mới|Upload new version\nQuản lý phiên bản|Version management\nMỗi bản slide lưu riêng. Nội dung cũ giữ nguồn gốc; phần thay đổi cần được dựng lại và duyệt.|Slide versions are stored separately. Existing content retains its sources; changes must be rebuilt and reviewed.\nXem nội dung cần bổ sung →|View content gaps →\nCase thật phải có bài báo nguồn. Tình huống giả định được gắn nhãn riêng.|Real cases require source articles. Hypothetical scenarios are labeled separately.\nHàng đợi thiếu nội dung|Content gap queue\nNhận việc|Claim task\nChưa có nội dung thiếu được ghi nhận|No content gaps recorded\nKhái niệm mới, nguồn thiếu và tìm kiếm không có kết quả sẽ được theo dõi tại đây.|New concepts, missing sources and unsuccessful searches are tracked here.\nĐiều chỉnh coin|Adjust coins\nGhi sổ coin|Record coin change\nGói|Package\nGiá VND|Price in VND\nĐang mở|Active\nChi phí AI thực tế|Actual AI costs\nSố cuộc gọi|Calls\nChi phí USD|Cost in USD\nChưa phát sinh chi phí AI.|No AI costs recorded yet.\nPhân quyền hệ thống|System permissions\nID nhân sự đã thiết lập Authenticator|Staff ID with Authenticator configured\nCập nhật vai trò|Update role\nNgười học không có quyền truy cập khu quản trị. Phát hành nội dung yêu cầu ContentReviewer hoặc SuperAdmin.|Students cannot access the admin area. Publishing requires ContentReviewer or SuperAdmin.\nKhóa|Lock\nMở khóa|Unlock\nGiả định|Hypothetical\nThử trước|Try it\nRẻ nhất mỗi coin|Lowest price per coin\n♧ Nhận thông báo khi mở|♧ Notify me at launch\nBản xem trước của gói mở rộng EduPlay VIP. Chưa mở, chưa có giá; mở sau giai đoạn pilot.|Preview of EduPlay VIP extras. Not available or priced yet; launches after the pilot.\n".trim().split('\n').map(line=>line.split('|'))));
 Object.assign(dictionary,{"ID tài khoản nhân sự":"Staff account ID","Email hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa. Sai 5 lần sẽ khóa 15 phút.":"Invalid email or password, or account locked. Five failed attempts lock access for 15 minutes."});
 const t=value=>{
  if(language!=='en')return value;
  if(dictionary[value])return dictionary[value];
  return value.replace(/^(\d+) coin ví$/,'$1 wallet coins').replace(/^(\d+) lượt$/,'$1 requests').replace(/^\/ ngày$/,'/ day').replace(/^([+\d]+)\/bạn$/,'$1/friend').replace(/coin\/3 câu/g,'coins/3 questions').replace(/^Kỳ (\d+)$/,'Semester $1').replace(/^Ngày (\d+)$/,'Day $1').replace(/^Chương (\d+)/,'Chapter $1').replace(/^MKT101 · CHƯƠNG (\d+)$/,'MKT101 · CHAPTER $1').replace(/^Chuỗi ×/,'Streak ×').replace(/^Bạn trả lời đúng (\d+)\/(\d+) câu. Kết quả đã được lưu.$/,'You answered $1/$2 questions correctly. Your results have been saved.').replace(/^Chào (.+) · Mỗi ngày một chút tiến bộ$/,'Hello $1 · A little progress every day');
 };
 window.eduText=t;
 const originals=new WeakMap();
 function translate(root=document.body){
  const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT,{acceptNode:n=>n.parentElement?.closest('script,style,[data-language],textarea,[contenteditable]')?NodeFilter.FILTER_REJECT:NodeFilter.FILTER_ACCEPT});
  const nodes=[];while(walker.nextNode())nodes.push(walker.currentNode);
  for(const node of nodes){const prior=originals.get(node);let source=prior&&node.nodeValue===prior.output?prior.source:node.nodeValue;const trimmed=source.trim();let output=source.replace(trimmed,t(trimmed));if(node.nodeValue!==output)node.nodeValue=output;originals.set(node,{source,output});}
  for(const el of root.querySelectorAll('[placeholder],[aria-label],[data-unavailable]'))for(const attr of ['placeholder','aria-label','data-unavailable']){if(!el.hasAttribute(attr))continue;const key='data-i18n-'+attr;let original=el.getAttribute(key);if(!original){original=el.getAttribute(attr);el.setAttribute(key,original);}const value=t(original);if(el.getAttribute(attr)!==value)el.setAttribute(attr,value);}
 }
 document.addEventListener('DOMContentLoaded',()=>{
  const toggle=document.querySelector('[data-theme-toggle]'),select=document.querySelector('[data-language]');
  const updateTheme=()=>{document.documentElement.dataset.theme=theme;if(toggle){toggle.textContent=theme==='dark'?'☀':'☾';toggle.setAttribute('aria-pressed',String(theme==='light'));}};
  toggle?.addEventListener('click',()=>{theme=theme==='dark'?'light':'dark';save('edu-theme',theme);updateTheme();});
  if(select){select.value=language;select.addEventListener('change',()=>{language=select.value;save('edu-language',language);document.documentElement.lang=language;translate();document.title=t(document.title.split(' · ')[0])+' · EduPlay';});}
  updateTheme();translate();document.title=t(document.title.split(' · ')[0])+' · EduPlay';
  let queued=false;new MutationObserver(()=>{if(queued)return;queued=true;queueMicrotask(()=>{queued=false;translate();});}).observe(document.body,{childList:true,subtree:true,characterData:true});
 });
})();
