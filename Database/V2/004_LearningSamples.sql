-- Original educational examples, explicitly marked sample, not lecturer slides or a verified news case.
USE EduPlayV2;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=4) BEGIN COMMIT; RETURN; END;
DECLARE @Ch int=(SELECT ChapterId FROM dbo.Chapters WHERE CourseId=(SELECT CourseId FROM dbo.Courses WHERE Code='MKT101') AND ChapterNumber=4);
-- Sample library data has no uploader account and no production slide version.
INSERT dbo.Documents(ChapterId,Scope,Kind,OriginalName,StorageKey,ByteLength,PageCount,UnlockedPages)
VALUES(@Ch,'Library','Slides',N'Pricing Strategies — nội dung mẫu','samples/pricing-text',1,5,5);
DECLARE @Doc bigint=SCOPE_IDENTITY();
INSERT dbo.Slides(DocumentId,PageNumber,ExtractedText) VALUES
(@Doc,1,N'New-Product Pricing Strategies\nPenetration pricing: đặt giá thấp khi ra mắt để thu hút khách hàng và tăng thị phần. Đây là nội dung học mẫu, không phải slide giảng viên.'),
(@Doc,2,N'Market-skimming pricing\nĐặt giá cao lúc ra mắt cho nhóm sẵn sàng chi trả, rồi giảm dần để tiếp cận nhóm khách hàng khác.'),
(@Doc,3,N'Bundle pricing\nKết hợp nhiều sản phẩm trong một gói với giá hấp dẫn hơn mua riêng.'),
(@Doc,4,N'Value-based pricing\nXây dựng mức giá dựa trên giá trị mà khách hàng cảm nhận.'),
(@Doc,5,N'Psychological pricing\nCách trình bày giá có thể ảnh hưởng đến cảm nhận và quyết định của khách hàng.');
DECLARE @Course int=(SELECT CourseId FROM dbo.Courses WHERE Code='MKT101');
DECLARE @N int=1;
WHILE @N<=5 BEGIN
 DECLARE @Term nvarchar(200),@Vn nvarchar(200),@Def nvarchar(2000),@Slide bigint,@Concept int,@Content bigint;
 SELECT @Term=t,@Vn=v,@Def=d FROM(VALUES
 (1,N'Penetration Pricing',N'Định giá thâm nhập',N'Đặt giá thấp lúc ra mắt để thu hút nhiều khách hàng và tăng thị phần.'),
 (2,N'Price Skimming',N'Định giá hớt váng',N'Đặt giá cao lúc ra mắt, sau đó giảm dần để tiếp cận các nhóm khách hàng.'),
 (3,N'Bundle Pricing',N'Định giá trọn gói',N'Bán nhiều sản phẩm trong một gói với giá hấp dẫn hơn mua riêng.'),
 (4,N'Value-based Pricing',N'Định giá theo giá trị',N'Xây dựng mức giá theo giá trị mà khách hàng cảm nhận.'),
 (5,N'Psychological Pricing',N'Định giá tâm lý',N'Trình bày giá theo cách tác động đến cảm nhận của khách hàng.'))x(n,t,v,d) WHERE n=@N;
 SELECT @Slide=SlideId FROM dbo.Slides WHERE DocumentId=@Doc AND PageNumber=@N;
 INSERT dbo.Concepts(CourseId,EnglishTerm,VietnameseTerm,Definition) VALUES(@Course,@Term,@Vn,@Def); SET @Concept=SCOPE_IDENTITY();
 INSERT dbo.ConceptSlides(ConceptId,SlideId,SourceQuote) VALUES(@Concept,@Slide,@Def);
 INSERT dbo.ContentItems(ChapterId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@Ch,@Doc,'Flashcard',@Term,'Published',SYSUTCDATETIME()); SET @Content=SCOPE_IDENTITY();
 INSERT dbo.Flashcards(ContentId,Front,Back) VALUES(@Content,@Term,@Def);
 INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@Content,@Slide);
 INSERT dbo.ContentConcepts(ContentId,ConceptId) VALUES(@Content,@Concept);
 INSERT dbo.ContentItems(ChapterId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@Ch,@Doc,'Question',CONCAT(N'Nhận diện ',@Term),'Published',SYSUTCDATETIME()); SET @Content=SCOPE_IDENTITY();
 INSERT dbo.Questions(ContentId,Format,Prompt,Explanation) VALUES(@Content,'MultipleChoice',CONCAT(N'Mô tả nào phù hợp với ',@Term,N'?'),@Def);
 INSERT dbo.QuestionOptions(QuestionId,Position,Text,IsCorrect) VALUES(@Content,1,@Def,1),(@Content,2,N'Giữ nguyên mọi mức giá trong tất cả các thị trường.',0),(@Content,3,N'Chỉ quyết định giá dựa trên màu sắc bao bì.',0),(@Content,4,N'Không xem xét nhu cầu hoặc cảm nhận của khách hàng.',0);
 INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@Content,@Slide);
 INSERT dbo.ContentConcepts(ContentId,ConceptId) VALUES(@Content,@Concept);
 SET @N+=1;
END;
INSERT dbo.ContentItems(ChapterId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@Ch,@Doc,'Case',N'Hãng sữa mới ra mắt — tình huống giả định','Published',SYSUTCDATETIME());
DECLARE @Case bigint=SCOPE_IDENTITY();
INSERT dbo.Cases(ContentId,CompanyName,IsHypothetical,Premise) VALUES(@Case,N'Hãng sữa A (giả định)',1,N'Bạn là thực tập sinh marketing. Một hãng sữa mới muốn nhanh chóng thu hút khách hàng khi ra mắt sản phẩm. Hãy chọn và vận dụng chiến lược giá.');
INSERT dbo.ContentSources(ContentId,SlideId) SELECT @Case,SlideId FROM dbo.Slides WHERE DocumentId=@Doc AND PageNumber=1;
INSERT dbo.ContentConcepts(ContentId,ConceptId) SELECT @Case,ConceptId FROM dbo.Concepts WHERE CourseId=@Course AND EnglishTerm=N'Penetration Pricing';
SET @N=1;
WHILE @N<=4 BEGIN
 DECLARE @Step bigint,@Prompt nvarchar(1000),@Correct nvarchar(1000),@Skill varchar(20);
 SELECT @Skill=s,@Prompt=p,@Correct=c FROM(VALUES
 (1,'Identify',N'Hãng muốn có nhiều khách hàng thật nhanh. Chiến lược nào phù hợp?',N'Penetration Pricing — giá thấp khi ra mắt'),
 (2,'Explain',N'Vì sao giá thấp khi ra mắt có thể hỗ trợ mục tiêu này?',N'Giảm rào cản thử sản phẩm và thu hút người mua nhạy cảm về giá'),
 (3,'Connect',N'Khái niệm nào trên slide liên quan trực tiếp?',N'Tăng thị phần thông qua định giá thâm nhập'),
 (4,'Apply',N'Bạn sẽ triển khai và theo dõi như thế nào?',N'Thử ở một thị trường, đo lượng mua và biên lợi nhuận trước khi mở rộng'))x(n,s,p,c) WHERE n=@N;
 INSERT dbo.CaseSteps(CaseId,StepNumber,Skill,Speaker,Prompt) VALUES(@Case,@N,@Skill,N'Chị Lan · Trưởng phòng Marketing',@Prompt); SET @Step=SCOPE_IDENTITY();
 INSERT dbo.CaseOptions(CaseStepId,Position,Text,Feedback,Quality) VALUES
 (@Step,1,@Correct,N'Phù hợp mục tiêu thâm nhập thị trường. Luôn theo dõi chi phí và biên lợi nhuận.',1),
 (@Step,2,N'Đặt giá cao nhất có thể và không đo phản hồi.',N'Phương án này chưa phù hợp mục tiêu thu hút nhanh khách hàng nhạy cảm về giá.',0),
 (@Step,3,N'Đổi toàn bộ bao bì và bỏ qua chiến lược giá.',N'Bao bì có thể hỗ trợ thương hiệu nhưng chưa giải quyết trực tiếp mục tiêu giá.',0);
 SET @N+=1;
END;
INSERT dbo.SchemaVersions(Version) VALUES(4);
COMMIT;
GO
