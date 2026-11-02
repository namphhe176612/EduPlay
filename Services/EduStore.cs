using Microsoft.Data.SqlClient;
using System.Data;
using PlayEdu.Models;
namespace PlayEdu.Services;

public class EduStore(IConfiguration config) {
 public SqlConnection Connection() => new(config.GetConnectionString("EduPlayV2") ?? "Server=localhost;Database=EduPlayV2;Trusted_Connection=True;TrustServerCertificate=True");
 public static SqlCommand Command(SqlConnection c,string sql,params (string,object?)[] args) {
  var cmd=new SqlCommand(sql,c);
  foreach(var (key,value) in args) cmd.Parameters.AddWithValue(key,value ?? DBNull.Value);
  return cmd;
 }
 public async Task<List<Dictionary<string,object?>>> Query(string sql,params (string,object?)[] args) {
  await using var c=Connection(); await c.OpenAsync(); await using var cmd=Command(c,sql,args);
  await using var reader=await cmd.ExecuteReaderAsync(); var rows=new List<Dictionary<string,object?>>();
  while(await reader.ReadAsync()) { var row=new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase); for(int i=0;i<reader.FieldCount;i++) row[reader.GetName(i)]=reader.IsDBNull(i)?null:reader.GetValue(i); rows.Add(row); }
  return rows;
 }
 public async Task<int> Execute(string sql,params (string,object?)[] args) {
  await using var c=Connection(); await c.OpenAsync(); await using var cmd=Command(c,sql,args); return await cmd.ExecuteNonQueryAsync();
 }
 public async Task<long> Register(EduRegistration form,string hash) {
  await using var c=Connection(); await c.OpenAsync(); await using var tx=(SqlTransaction)await c.BeginTransactionAsync();
  await using var cmd=Command(c,"""
   DECLARE @Referrer bigint=(SELECT UserId FROM dbo.Users WHERE ReferralCode=@Referral);
   INSERT dbo.Users(FullName,Email,PasswordHash,UniversityId,MajorId,CurrentSemester,AcquisitionSource,ReferralCode,ReferredByUserId,MarketingConsent,TermsVersion,TermsAcceptedAt)
   VALUES(@Name,@Email,@Hash,(SELECT TOP 1 UniversityId FROM dbo.Universities),(SELECT MajorId FROM dbo.Majors WHERE Name=@Major),@Semester,@Source,@Code,@Referrer,@Consent,'2026-10',SYSUTCDATETIME());
   DECLARE @Id bigint=SCOPE_IDENTITY();
   INSERT dbo.Wallets(UserId) VALUES(@Id);
   INSERT dbo.UserSettings(UserId) VALUES(@Id);
   INSERT dbo.UserRoles(UserId,RoleId) SELECT @Id,RoleId FROM dbo.Roles WHERE Code='Learner';
   INSERT dbo.VaultSlots(UserId,SlotNumber) VALUES(@Id,1),(@Id,2),(@Id,3);
   DECLARE @Key varchar(150)=CONCAT('welcome:',@Id);
   EXEC dbo.ApplyCoinChange @Id,20,'Welcome',@Key;
   INSERT dbo.RewardClaims(UserId,RewardCode,ScopeKey,LedgerId) SELECT @Id,'Welcome','once',LedgerId FROM dbo.CoinLedger WHERE IdempotencyKey=@Key;
   IF @Referrer IS NOT NULL INSERT dbo.Referrals(ReferrerUserId,ReferredUserId) VALUES(@Referrer,@Id);
   SELECT @Id;
   """,("@Name",form.FullName.Trim()),("@Email",form.Email.Trim()),("@Hash",hash),
   ("@Major",form.Marketing?"Marketing":"Ngành khác"),("@Semester",form.Semester),("@Source",form.Source),
   ("@Code",Guid.NewGuid().ToString("N")[..12]),("@Referral",form.Referral),("@Consent",form.MarketingConsent));
  cmd.Transaction=tx; var id=Convert.ToInt64(await cmd.ExecuteScalarAsync()); await tx.CommitAsync(); return id;
 }
 public async Task<EduPage> Load(string page,long userId,bool preview,long id=0,string? query=null) {
  var vm=new EduPage { Page=page,UserId=userId,Preview=preview,Id=id,Query=query };
  if(userId>0) {
   var u=(await Query("SELECT u.FullName,w.Balance FROM dbo.Users u JOIN dbo.Wallets w ON w.UserId=u.UserId WHERE u.UserId=@u",("@u",userId))).FirstOrDefault();
   if(u!=null) { vm.Name=EduPage.Text(u,"FullName"); vm.Balance=(int)EduPage.Number(u,"Balance"); }
  }
  vm.Courses=await Query("SELECT c.*,CONVERT(bit,CASE WHEN e.EnrollmentId IS NULL THEN 0 ELSE 1 END) Enrolled FROM dbo.Courses c LEFT JOIN dbo.Enrollments e ON e.CourseId=c.CourseId AND e.UserId=@u AND e.IsActive=1 ORDER BY c.CourseId",("@u",userId));
  vm.Chapters=await Query("""
   SELECT ch.*,c.Code,COALESCE(r.Readiness,0) Readiness FROM dbo.Chapters ch JOIN dbo.Courses c ON c.CourseId=ch.CourseId
   LEFT JOIN dbo.vChapterReadiness r ON r.ChapterId=ch.ChapterId AND r.UserId=@u WHERE c.Code=@code ORDER BY ChapterNumber
   """,("@u",userId),("@code",query is "MKT201" or "MKT304" ? query : "MKT101"));
  switch(page) {
   case "today":
    vm.Data=(await Query("SELECT TOP 1 c.Code CourseCode,c.Name CourseName,ch.ChapterId,ch.ChapterNumber,ch.Title ChapterTitle,x.ExamDate FROM dbo.Enrollments e JOIN dbo.Courses c ON c.CourseId=e.CourseId LEFT JOIN dbo.Chapters ch ON ch.ChapterId=e.CurrentChapterId OUTER APPLY(SELECT TOP 1 ExamDate FROM dbo.Exams WHERE EnrollmentId=e.EnrollmentId AND ExamDate>=CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME())) ORDER BY ExamDate)x WHERE e.UserId=@u AND e.IsActive=1 ORDER BY e.EnrollmentId",("@u",userId))).FirstOrDefault()??[];
    vm.Extra=await Query("SELECT DISTINCT co.ConceptId,co.EnglishTerm,COALESCE(p.Mastery,0) Mastery FROM dbo.Concepts co JOIN dbo.ContentConcepts cc ON cc.ConceptId=co.ConceptId JOIN dbo.ContentItems i ON i.ContentId=cc.ContentId LEFT JOIN dbo.UserConceptProgress p ON p.UserId=@u AND p.ConceptId=co.ConceptId WHERE i.ChapterId=@ch AND i.OwnerUserId IS NULL AND i.Status='Published'",("@u",userId),("@ch",EduPage.Number(vm.Data,"ChapterId"))); break;
   case "slides":
    vm.Items=await Query("SELECT s.*,d.OriginalName FROM dbo.Slides s JOIN dbo.Documents d ON d.DocumentId=s.DocumentId LEFT JOIN dbo.SlideVersions v ON v.DocumentId=d.DocumentId WHERE d.DeletedAt IS NULL AND ((d.Scope='Library' AND (v.Status='Published' OR (v.SlideVersionId IS NULL AND d.StorageKey LIKE 'samples/%'))) OR d.OwnerUserId=@u) ORDER BY s.DocumentId,s.PageNumber",("@u",userId));
    vm.Data=vm.Items.FirstOrDefault(x=>EduPage.Number(x,"SlideId")==id)??vm.Items.FirstOrDefault()??[];
    vm.Extra=await Query("SELECT Note FROM dbo.SlideNotes WHERE UserId=@u AND SlideId=@s",("@u",userId),("@s",EduPage.Number(vm.Data,"SlideId"))); break;
   case "flashcards":
    vm.Items=await Query("SELECT i.*,f.Front,f.Back,MIN(s.SlideId) SlideId FROM dbo.ContentItems i JOIN dbo.Flashcards f ON f.ContentId=i.ContentId LEFT JOIN dbo.ContentSources s ON s.ContentId=i.ContentId WHERE i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u) AND (@chapter=0 OR i.ChapterId=@chapter) GROUP BY i.ContentId,i.ChapterId,i.OwnerUserId,i.SourceDocumentId,i.Kind,i.Title,i.Revision,i.SupersedesContentId,i.Status,i.CreatedBy,i.CreatedAt,i.PublishedAt,f.Front,f.Back",("@u",userId),("@chapter",id)); break;
   case "quiz": case "links":
    if(page=="quiz"&&(preview||query=="demo-five")) {
     vm.Items=await Query("SELECT i.ContentId,i.Title,q.Prompt FROM dbo.ContentItems i JOIN dbo.Questions q ON q.ContentId=i.ContentId WHERE i.Kind='Question' AND i.Status='Published' AND i.OwnerUserId IS NULL AND i.Title LIKE N'DEMO5 · Câu %' ORDER BY i.ContentId");
     if(preview)vm.Extra=await Query("SELECT q.ContentId,q.Prompt,q.Explanation,o.OptionId,o.Text,o.IsCorrect,(SELECT MIN(SlideId) FROM dbo.ContentSources WHERE ContentId=i.ContentId) SlideId FROM dbo.ContentItems i JOIN dbo.Questions q ON q.ContentId=i.ContentId JOIN dbo.QuestionOptions o ON o.QuestionId=q.ContentId WHERE i.Kind='Question' AND i.Status='Published' AND i.OwnerUserId IS NULL AND i.Title LIKE N'DEMO5 · Câu %' ORDER BY i.ContentId,o.Position");
    }else {
    vm.Items=await Query("SELECT i.ContentId,i.Title,q.Prompt FROM dbo.ContentItems i JOIN dbo.Questions q ON q.ContentId=i.ContentId WHERE i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u) AND (@chapter=0 OR i.ChapterId=@chapter) AND i.Kind=@kind ORDER BY i.ContentId",("@u",userId),("@chapter",id),("@kind",page=="links"?"ConceptLink":"Question"));
    }break;
   case "case": case "case-results":
    vm.Items=await Query("SELECT i.*,c.Premise,c.CompanyName,c.IsHypothetical FROM dbo.ContentItems i JOIN dbo.Cases c ON c.ContentId=i.ContentId WHERE i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u)",("@u",userId));
    vm.Data=vm.Items.FirstOrDefault(x=>EduPage.Number(x,"ContentId")==id)??vm.Items.FirstOrDefault()??[];
    vm.Extra=await Query("SELECT s.*,o.CaseOptionId,o.Position,o.Text,o.Feedback,o.Quality FROM dbo.CaseSteps s JOIN dbo.CaseOptions o ON o.CaseStepId=s.CaseStepId WHERE s.CaseId=@c ORDER BY s.StepNumber,o.Position",("@c",EduPage.Number(vm.Data,"ContentId"))); break;
   case "wallet":
    vm.Packages=await Query("SELECT * FROM dbo.CoinPackages WHERE IsActive=1 ORDER BY PriceVnd");
    vm.Items=await Query("SELECT TOP 50 * FROM dbo.CoinLedger WHERE UserId=@u ORDER BY LedgerId DESC",("@u",userId));
    vm.Extra=await Query("SELECT * FROM dbo.FeaturePolicies WHERE FreeUnits>0 AND Availability='Available'"); break;
   case "topup": case "pricing": vm.Items=await Query("SELECT * FROM dbo.CoinPackages WHERE IsActive=1 ORDER BY PriceVnd"); break;
   case "files":
    vm.Items=await Query("SELECT d.*,j.Status JobStatus FROM dbo.Documents d OUTER APPLY(SELECT TOP 1 Status FROM dbo.Jobs WHERE DocumentId=d.DocumentId ORDER BY JobId DESC)j WHERE d.OwnerUserId=@u AND d.DeletedAt IS NULL ORDER BY d.DocumentId DESC",("@u",userId)); break;
   case "vault":
    vm.Items=await Query("SELECT TOP 50 s.*,e.Kind,e.StartedAt,e.Score,e.Status,i.Title FROM dbo.SessionItems s JOIN dbo.StudySessions e ON e.SessionId=s.SessionId JOIN dbo.ContentItems i ON i.ContentId=s.ContentId WHERE e.UserId=@u ORDER BY s.SessionItemId DESC",("@u",userId));
    vm.Extra=await Query("SELECT w.*,q.Prompt FROM dbo.WrongQuestions w JOIN dbo.Questions q ON q.ContentId=w.QuestionId WHERE w.UserId=@u AND w.ResolvedAt IS NULL",("@u",userId)); break;
   case "settings":
    vm.Data=(await Query("SELECT s.*,u.Email,u.ReferralCode,u.UniversityId,u.CurrentSemester FROM dbo.UserSettings s JOIN dbo.Users u ON u.UserId=s.UserId WHERE s.UserId=@u",("@u",userId))).FirstOrDefault()??[]; break;
   case "progress":
    vm.Items=await Query("SELECT * FROM dbo.vChapterReadiness WHERE UserId=@u",("@u",userId));
    vm.Data=(await Query("SELECT (SELECT COUNT(*) FROM dbo.DailyActivity WHERE UserId=@u) StudyDays,(SELECT COUNT(*) FROM dbo.QuestionAnswers a JOIN dbo.SessionItems si ON si.SessionItemId=a.SessionItemId JOIN dbo.StudySessions ss ON ss.SessionId=si.SessionId WHERE ss.UserId=@u) Answers,(SELECT COUNT(*) FROM dbo.CaseCollection WHERE UserId=@u) Cases,(SELECT COUNT(*) FROM dbo.UserConceptProgress WHERE UserId=@u AND Mastery>=80) Mastered",("@u",userId))).First(); break;
   case "search":
    vm.Items=await Query("SELECT ConceptId,EnglishTerm,VietnameseTerm,Definition FROM dbo.Concepts WHERE EnglishTerm LIKE @q OR VietnameseTerm LIKE @q",("@q","%"+query+"%")); break;
  }
  return vm;
 }
}
