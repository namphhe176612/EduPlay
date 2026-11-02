from docx import Document
from docx.shared import Inches, Pt, RGBColor
from pathlib import Path
D=Document()
s=D.sections[0]; s.top_margin=s.bottom_margin=Inches(.7); s.left_margin=s.right_margin=Inches(.8)
normal=D.styles['Normal']; normal.font.name='Arial'; normal.font.size=Pt(11); normal.paragraph_format.space_after=Pt(7)
normal.paragraph_format.line_spacing=1.12
for name,size in [('Title',23),('Heading 1',14),('Heading 2',12)]:
 st=D.styles[name]; st.font.name='Arial'; st.font.size=Pt(size); st.font.color.rgb=RGBColor(0,0,0)
D.add_paragraph('Kịch bản demo hệ thống PlayEdu','Title')
D.add_paragraph('Thời lượng 5–7 phút  |  Giới thiệu hệ thống và trải nghiệm học từ PDF')
D.add_paragraph('Mục tiêu buổi demo là giới thiệu PlayEdu và trình diễn một luồng học hoàn chỉnh: tải bài giảng, dựng sa bàn, đưa ra quyết định, xem kết quả và đối chiếu lại tài liệu nguồn.')
D.add_heading('Chuẩn bị trước buổi demo',1)
D.add_paragraph('Mở http://localhost:5210 khi ứng dụng và SQL Server đã chạy. Chuẩn bị tài khoản người học đã đăng nhập, một PDF có văn bản từ 1–15 trang và một lượt chơi đã hoàn thành để dự phòng. Dùng chế độ Demo mặc định; kiểm tra trước PDF trích xuất được chữ. Không dùng PDF scan vì hệ thống chưa có OCR.')
def step(title,action,speech):
 D.add_heading(title,1)
 p=D.add_paragraph(); p.add_run('Thao tác  ').bold=True; p.add_run(action)
 p=D.add_paragraph(); p.add_run('Lời nói  ').bold=True; p.add_run('“'+speech+'”')
step('1 Mở đầu và giới thiệu hệ thống khoảng 45 giây','Mở trang chủ, sau đó chuyển vào Không gian học tập.','Em xin chào thầy cô và các bạn. Hôm nay em xin giới thiệu PlayEdu, một hệ thống hỗ trợ học từ tài liệu PDF thông qua các tình huống tương tác. Ý tưởng của hệ thống là giúp người học vừa đọc kiến thức, vừa thực hành lựa chọn và xem hệ quả của quyết định. Sau mỗi lượt học, người dùng có thể xem lại kết quả và mở đúng trang tài liệu để kiểm tra căn cứ.')
step('2 Tổng quan cách hoạt động khoảng 30 giây','Giới thiệu nhanh các mục tài liệu, hồ sơ và ví trên giao diện.','Luồng sử dụng chính gồm tải tài liệu, dựng sa bàn, vào vai xử lý tình huống và xem kết quả. Hệ thống có tài khoản người học và quản trị viên. Về kỹ thuật, ứng dụng dùng ASP.NET Core MVC, cơ sở dữ liệu SQL Server và PdfPig để trích xuất văn bản từ PDF. Bản trình diễn hôm nay đang chạy chế độ Demo, sử dụng tình huống theo mẫu có gắn trích đoạn từ tài liệu, chưa gọi AI thật.')
step('3 Tải tài liệu và kiểm tra nguồn khoảng 60 giây','Vào Tài liệu của tôi → tải PDF → mở chi tiết → chọn một trang và chỉ vào phần Văn bản slide.','Đầu tiên, em tải lên một bài giảng PDF. Sau khi xử lý, hệ thống hiển thị tài liệu gốc và phần văn bản được trích xuất theo từng trang. Người học có thể kiểm tra nội dung trước khi bắt đầu. Tài liệu được quản lý theo tài khoản; người dùng khác không có quyền mở tài liệu này. Với file từ 1 đến 15 trang như ví dụ này, việc tải lên là miễn phí.')
D.add_page_break()
D.add_heading('Trình diễn luồng học và kết thúc',1)
step('4 Dựng sa bàn và vào vai khoảng 60 giây','Tại chi tiết tài liệu, bấm Dựng sa bàn · 0 coin. Xem vai trò, bối cảnh và bốn tình huống, rồi bấm Vào vai.','Từ tài liệu vừa tải lên, em chọn dựng sa bàn. Ở chế độ Demo, hệ thống tạo bốn tình huống theo mẫu và sử dụng trích đoạn của PDF làm nguồn tham chiếu. Ô bối cảnh tùy chỉnh chưa được sử dụng trong chế độ này. Trước khi chơi, người học xem vai trò, bối cảnh và các chỉ số mô phỏng để hiểu nhiệm vụ của mình.')
step('5 Đưa ra lựa chọn khoảng 60 đến 90 giây','Đọc một tình huống, giải thích ngắn căn cứ chọn một phương án, chỉ vào hậu quả và chỉ số thay đổi. Hoàn thành các tình huống còn lại.','Ở mỗi tình huống, người học có ba phương án để lựa chọn. Em chọn phương án này dựa trên nội dung vừa xem trong tài liệu. Sau khi xác nhận, hệ thống ghi nhận quyết định và hiển thị hậu quả cùng thay đổi của các chỉ số. Mỗi bước chỉ ghi nhận một lựa chọn; muốn thử cách xử lý khác, người học có thể chơi lại sau khi hoàn thành.')
step('6 Xem kết quả và đối chiếu khoảng 60 giây','Mở kết quả, chỉ vào điểm /80, mở một slide nguồn và các phương án còn lại. Gửi một câu hỏi có từ khóa trong PDF, sau đó bấm Lưu vào kho.','Kết quả tổng hợp các quyết định và chỉ số của lượt chơi. Thang điểm hiện tại là 80, gồm 60 điểm quyết định và 20 điểm chỉ số mô phỏng; phần chấm lập luận bằng AI chưa triển khai. Đây là kết quả minh họa theo mẫu, chưa dùng để đánh giá năng lực thực tế. Điểm đáng chú ý là mỗi quyết định đều có giải thích và liên kết về trang nguồn. Người học cũng có thể xem phương án khác, hỏi để tìm trích đoạn liên quan và lưu kết quả vào kho. Trong Demo, phần hỏi đáp chỉ tìm trích đoạn, chưa tạo câu trả lời bằng AI.')
step('7 Giới thiệu mở rộng và kết thúc khoảng 30 giây','Chỉ nhanh nút Luyện tập sâu; kết thúc tại trang kết quả.','Ngoài luồng vừa trình diễn, PlayEdu có bài luyện tập, hồ sơ học tập, ví coin và chức năng quản trị người dùng. Qua buổi demo, em muốn làm rõ cách hệ thống kết nối tài liệu với thực hành và hỗ trợ người học kiểm tra lại nguồn kiến thức. Định hướng tiếp theo là kiểm thử tích hợp AI thật và hoàn thiện các chức năng nâng cao. Em xin kết thúc phần trình bày và sẵn sàng nhận câu hỏi.')
D.add_heading('Nếu thao tác bị gián đoạn',2)
D.add_paragraph('Chuyển sang lượt chơi đã chuẩn bị và nói: “Để tiếp tục minh họa đầy đủ luồng học, em sẽ mở kết quả đã chuẩn bị từ trước.” Nếu được hỏi về AI, nêu rõ bản Demo theo mẫu; tích hợp provider thật chưa được kiểm thử. Thanh toán thật và chấm lập luận bằng AI chưa triển khai.')
D.save(Path('outputs/Kich_ban_demo_PlayEdu.docx'))
