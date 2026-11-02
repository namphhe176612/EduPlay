USE EduPlayV2;
GO
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @chapter int=(SELECT ch.ChapterId FROM dbo.Chapters ch JOIN dbo.Courses c ON c.CourseId=ch.CourseId WHERE c.Code='MKT101' AND ch.ChapterNumber=4);
DECLARE @doc bigint=(SELECT TOP 1 DocumentId FROM dbo.Documents WHERE StorageKey='samples/pricing-text' AND DeletedAt IS NULL);
IF @chapter IS NULL OR @doc IS NULL THROW 50001,N'Missing sample chapter or source slides.',1;
DECLARE @questions TABLE(n int,term nvarchar(200),prompt nvarchar(2000),a nvarchar(1000),b nvarchar(1000),c nvarchar(1000),d nvarchar(1000),answer int,explanation nvarchar(2000));
INSERT @questions VALUES
(1,N'Penetration Pricing',N'Một thương hiệu sữa mới muốn nhanh chóng thu hút khách hàng nhạy cảm về giá và tăng thị phần. Chiến lược nào phù hợp?',N'Đặt giá cao lúc ra mắt rồi giảm dần',N'Đặt giá thấp lúc ra mắt để khuyến khích dùng thử',N'Chỉ bán nhiều sản phẩm thành một gói',N'Chọn giá ngẫu nhiên mỗi ngày',2,N'Định giá thâm nhập dùng mức giá thấp ban đầu để giảm rào cản mua thử và tăng thị phần. Cần theo dõi chi phí và biên lợi nhuận; giá thấp không tự bảo đảm thành công.'),
(2,N'Price Skimming',N'Một hãng điện thoại đặt giá cao cho nhóm khách sẵn sàng trả nhiều khi ra mắt, rồi giảm dần để tiếp cận nhóm khác. Đây là chiến lược gì?',N'Định giá hớt váng',N'Định giá thâm nhập',N'Định giá trọn gói',N'Giữ giá cố định cho mọi thời điểm',1,N'Định giá hớt váng bắt đầu bằng giá cao cho nhóm có mức sẵn sàng chi trả cao, sau đó giảm dần. Điểm phân biệt với thâm nhập là mức giá và nhóm khách hàng mục tiêu lúc ra mắt.'),
(3,N'Bundle Pricing',N'Quán cà phê bán một ly nước 40.000 đồng và bánh 30.000 đồng. Combo cả hai có giá 60.000 đồng. Đây là ví dụ của cách định giá nào?',N'Định giá theo chi phí',N'Định giá hớt váng',N'Định giá trọn gói',N'Định giá thâm nhập',3,N'Định giá trọn gói kết hợp nhiều sản phẩm với tổng giá hấp dẫn hơn mua riêng. Trong ví dụ, mua riêng là 70.000 đồng, combo là 60.000 đồng, tiết kiệm 10.000 đồng.'),
(4,N'Value-based Pricing',N'Doanh nghiệp xác định mức giá chủ yếu dựa trên lợi ích mà khách hàng cảm nhận, thay vì chỉ lấy chi phí cộng lãi. Cách tiếp cận này gọi là gì?',N'Định giá ngẫu nhiên',N'Định giá theo đối thủ duy nhất',N'Định giá trọn gói',N'Định giá theo giá trị',4,N'Định giá theo giá trị xuất phát từ giá trị và lợi ích khách hàng cảm nhận. Doanh nghiệp vẫn cần xem xét chi phí và thị trường khi kiểm tra tính khả thi của mức giá.'),
(5,N'Psychological Pricing',N'Cửa hàng niêm yết 199.000 đồng thay cho 200.000 đồng nhằm tác động đến cảm nhận về mức giá. Đây là ví dụ điển hình của cách định giá nào?',N'Định giá hớt váng',N'Định giá tâm lý',N'Định giá trọn gói',N'Định giá theo chi phí',2,N'Định giá tâm lý chú ý cách trình bày giá ảnh hưởng đến cảm nhận của người mua. Mức 199.000 đồng là ví dụ giá kết thúc bằng số 9; hiệu quả thực tế còn tùy khách hàng và bối cảnh.');
DECLARE @n int=1;
WHILE @n<=5 BEGIN
 DECLARE @title nvarchar(250)=CONCAT(N'DEMO5 · Câu ',@n),@id bigint,@slide bigint,@concept int;
 SELECT @id=NULL,@slide=NULL,@concept=NULL;
 SELECT @id=ContentId FROM dbo.ContentItems WITH(UPDLOCK,HOLDLOCK) WHERE Title=@title AND Kind='Question' AND OwnerUserId IS NULL;
 IF @id IS NULL BEGIN
  INSERT dbo.ContentItems(ChapterId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@chapter,@doc,'Question',@title,'Published',SYSUTCDATETIME());SET @id=SCOPE_IDENTITY();
  INSERT dbo.Questions(ContentId,Format,Prompt,Explanation) SELECT @id,'MultipleChoice',prompt,explanation FROM @questions WHERE n=@n;
  INSERT dbo.QuestionOptions(QuestionId,Position,Text,IsCorrect)
  SELECT @id,v.pos,v.txt,CONVERT(bit,CASE WHEN v.pos=q.answer THEN 1 ELSE 0 END) FROM @questions q CROSS APPLY(VALUES(1,q.a),(2,q.b),(3,q.c),(4,q.d))v(pos,txt) WHERE q.n=@n;
  SELECT @slide=SlideId FROM dbo.Slides WHERE DocumentId=@doc AND PageNumber=@n;
  INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@id,@slide);
  SELECT @concept=c.ConceptId FROM dbo.Concepts c JOIN @questions q ON q.term=c.EnglishTerm WHERE q.n=@n AND c.CourseId=(SELECT CourseId FROM dbo.Chapters WHERE ChapterId=@chapter);
  IF @concept IS NOT NULL INSERT dbo.ContentConcepts(ContentId,ConceptId) VALUES(@id,@concept);
 END;
 SET @n+=1;
END;
COMMIT;
SELECT i.ContentId,q.Prompt FROM dbo.ContentItems i JOIN dbo.Questions q ON q.ContentId=i.ContentId WHERE i.Title LIKE N'DEMO5 · Câu %' AND i.OwnerUserId IS NULL ORDER BY i.ContentId;
GO
