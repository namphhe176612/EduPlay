USE EduPlayV2;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=2)
BEGIN
 COMMIT;
 RETURN;
END;
INSERT dbo.Universities(Name) VALUES(N'FPT University');
INSERT dbo.Majors(Name) VALUES(N'Marketing'),(N'Ngành khác');
INSERT dbo.Courses(Code,Name,MajorId) SELECT 'MKT101',N'Principles of Marketing',MajorId FROM dbo.Majors WHERE Name=N'Marketing';
-- Other courses are placeholders in the screenshots; do not invent their curriculum.
INSERT dbo.Courses(Code,Name,MajorId,IsAvailable) SELECT v.Code,v.Code,MajorId,0 FROM dbo.Majors
 CROSS JOIN(VALUES('MKT201'),('MKT304'))v(Code) WHERE Name=N'Marketing';
INSERT dbo.Chapters(CourseId,ChapterNumber,Title,WeekFrom,WeekTo)
 SELECT CourseId,v.N,v.Title,v.W1,v.W2 FROM dbo.Courses CROSS JOIN(VALUES
 (1,N'What is Marketing?',1,2),(2,N'The Marketing Environment',3,3),
 (3,N'Products, Services & Brands',4,5),(4,N'Pricing Strategies',6,6),(5,N'Distribution Channels',7,8))v(N,Title,W1,W2)
 WHERE Code='MKT101';
INSERT dbo.Roles(Code) VALUES('Learner'),('ContentEditor'),('ContentReviewer'),('SuperAdmin');
INSERT dbo.Permissions(Code) VALUES('content.read'),('content.edit'),('content.publish'),('reports.resolve'),
 ('slides.manage'),('cases.manage'),('gaps.manage'),('users.manage'),('coins.adjust'),('pricing.manage'),('finance.read'),('roles.manage');
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.RoleId,p.PermissionId FROM dbo.Roles r CROSS JOIN dbo.Permissions p
 WHERE r.Code='SuperAdmin' OR (r.Code='ContentEditor' AND p.Code IN('content.read','content.edit','slides.manage','cases.manage','gaps.manage'))
 OR (r.Code='ContentReviewer' AND p.Code IN('content.read','content.publish','reports.resolve','gaps.manage'));
INSERT dbo.CoinPackages(Code,Name,Coins,PriceVnd) VALUES('STARTER',N'Starter',50,19000),('STANDARD',N'Standard',120,39000),('SAVER',N'Saver',300,79000);
INSERT dbo.FeaturePolicies(FeatureCode,Name,Availability,FreeUnits,ResetPeriod,CoinsPerBlock,UnitsPerBlock) VALUES
 ('CaseChat',N'Chất vấn AI','Available',3,'PerCase',1,1),
 ('SlideHint',N'Gợi ý slide','Available',0,'None',1,1),
 ('DictionaryAI',N'Tra từ AI','Available',10,'Daily',1,1),
 ('DeepPractice',N'Học sâu','Available',6,'Daily',1,3),
 ('GuideAI',N'Trợ lý hướng dẫn','Available',20,'Daily',0,1),
 ('BuildCase',N'Dựng case từ slide','Available',0,'None',3,1),
 ('UnlockPdf',N'Mở PDF đến 40 trang','Available',15,'None',15,1),
 ('VaultSlot',N'Thêm ô lưu','Available',3,'Lifetime',10,1),
 ('Video90',N'Video 90 giây','ComingSoon',0,'None',0,1),
 ('StudyPlan',N'Lộ trình ôn thi','ComingSoon',0,'None',0,1),
 ('MockExam',N'Thi thử','ComingSoon',0,'None',0,1),
 ('StudyPack',N'Bộ ôn theo đề cương','ComingSoon',0,'None',0,1),
 ('PdfExport',N'Xuất đề cương PDF','ComingSoon',0,'None',0,1);
INSERT dbo.SystemSettings(SettingKey,Value,Description) VALUES
 ('Upload.MaxBytes','20971520',N'Giữ mức 20 MB của hệ thống cũ; ảnh mới chưa chốt.'),
 ('Upload.MaxPages','40',N'Từ chối PDF trên 40 trang; không tự thu thêm coin.'),
 ('Upload.FreePages','15',N'Miễn phí 15 trang đầu, mở phần còn lại bằng một giao dịch 15 coin.'),
 ('Vault.FreeSlots','3',N'Ô lưu kết quả case, không dùng chung PDF riêng.'),
 ('Vault.MaxSlots','9',N'Tối đa 9 ô theo bảng giá mới.'),
 ('Reward.Welcome','20',N'Thưởng đăng ký một lần.'),
 ('Reward.FirstCase','1',N'Mỗi case chỉ thưởng lần hoàn thành đầu tiên.'),
 ('Reward.Streak7','5',N'Cần quy tắc mốc tuần khi tích hợp ứng dụng.'),
 ('Reward.ValidReport','5',N'Thưởng sau khi người duyệt xác nhận lỗi.'),
 ('Reward.Referral','10',N'Chỉ thưởng sau buổi học đầu của người được mời.'),
 ('Reports.SlaHours','48',N'Thời hạn phản hồi báo lỗi.'),
 ('Quota.TimeZone','Asia/Bangkok',N'Đổi ngày hạn mức lúc 00:00 giờ địa phương, không dùng ngày UTC.'),
 ('AI.MaxRevenueRatio','0.30',N'Mục tiêu chi phí AI tối đa 30% doanh thu.'),
 ('Content.MinCaseScore','0.60',N'Điểm tối thiểu trước kiểm duyệt; không tự phát hành.'),
 ('Account.DeleteGraceDays','7',N'Thời gian chờ xóa tài khoản.'),
 ('Admin.LockoutAttempts','5',N'Số lần sai liên tiếp trước khi khóa tạm.'),
 ('Admin.LockoutMinutes','15',N'Thời gian khóa đăng nhập quản trị.');
INSERT dbo.SchemaVersions(Version) VALUES(2);
COMMIT;
GO
