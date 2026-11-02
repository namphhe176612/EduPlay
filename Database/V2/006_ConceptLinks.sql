USE EduPlayV2;
GO
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;
IF EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=6) BEGIN COMMIT;RETURN;END;
DECLARE @course int=(SELECT CourseId FROM dbo.Courses WHERE Code='MKT101'),@chapter int;
SELECT @chapter=ChapterId FROM dbo.Chapters WHERE CourseId=@course AND ChapterNumber=4;
INSERT dbo.Concepts(CourseId,EnglishTerm,VietnameseTerm,Definition) VALUES
(@course,N'Market Share',N'Thị phần',N'Tỷ trọng doanh số hoặc lượng bán của doanh nghiệp trong thị trường.'),
(@course,N'Customer Value',N'Giá trị khách hàng cảm nhận',N'Lợi ích mà khách hàng cảm nhận so với chi phí bỏ ra.'),
(@course,N'Product Bundle',N'Gói sản phẩm',N'Nhiều sản phẩm được bán cùng nhau trong một gói.');
DECLARE @from int,@to int,@question bigint,@slide bigint,@doc bigint,@n int=1;
WHILE @n<=3 BEGIN
 DECLARE @term nvarchar(200),@target nvarchar(200),@prompt nvarchar(1000),@explanation nvarchar(2000);
 SELECT @term=t,@target=target,@prompt=p,@explanation=e FROM(VALUES
 (1,N'Penetration Pricing',N'Market Share',N'Khái niệm nào là mục tiêu trực tiếp của định giá thâm nhập?',N'Giá thấp khi ra mắt nhằm thu hút nhiều khách hàng và tăng thị phần.'),
 (2,N'Value-based Pricing',N'Customer Value',N'Định giá theo giá trị gắn trực tiếp với yếu tố nào?',N'Mức giá dựa trên giá trị mà khách hàng cảm nhận từ sản phẩm.'),
 (3,N'Bundle Pricing',N'Product Bundle',N'Định giá trọn gói gắn trực tiếp với cấu trúc bán hàng nào?',N'Nhiều sản phẩm được kết hợp thành gói, thường với giá hấp dẫn hơn mua riêng.'))v(n,t,target,p,e) WHERE n=@n;
 SELECT @from=ConceptId FROM dbo.Concepts WHERE CourseId=@course AND EnglishTerm=@term;
 SELECT @to=ConceptId FROM dbo.Concepts WHERE CourseId=@course AND EnglishTerm=@target;
 SELECT TOP 1 @slide=s.SlideId,@doc=s.DocumentId FROM dbo.ConceptSlides cs JOIN dbo.Slides s ON s.SlideId=cs.SlideId WHERE cs.ConceptId=@from ORDER BY s.SlideId;
 INSERT dbo.ContentItems(ChapterId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@chapter,@doc,'ConceptLink',CONCAT(@term,N' → ',@target),'Published',SYSUTCDATETIME());SET @question=SCOPE_IDENTITY();
 INSERT dbo.ConceptLinks(ContentId,FromConceptId,ToConceptId,Explanation) VALUES(@question,@from,@to,@explanation);
 INSERT dbo.Questions(ContentId,Format,Prompt,Explanation) VALUES(@question,'MultipleChoice',@prompt,@explanation);
 INSERT dbo.QuestionOptions(QuestionId,Position,Text,IsCorrect) VALUES(@question,1,@target,1),(@question,2,N'Brand Identity',0),(@question,3,N'Sponsorship',0),(@question,4,N'Packaging Design',0);
 INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@question,@slide);
 INSERT dbo.ContentConcepts(ContentId,ConceptId) VALUES(@question,@from);
 SET @n+=1;
END;
INSERT dbo.SchemaVersions(Version) VALUES(6);
COMMIT;
GO
