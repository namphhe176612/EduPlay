using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using PlayEdu.Models;
using PlayEdu.Services;
using UglyToad.PdfPig;
namespace PlayEdu.Controllers;

[Route("edu")]
public class EduController(EduStore store,IWebHostEnvironment env,AiGateway ai) : Controller {
 const string Scheme="EduV2";
 long Uid => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var n)?n:0;
 static readonly HashSet<string> Pages=["today","tools","courses","slides","flashcards","quiz","links","case","case-results","plan","mock","study-pack","export","video","vault","files","progress","wallet","topup","pricing","settings","states","onboarding","search","admin","admin-reports","admin-review","admin-slides","admin-cases","admin-gaps","admin-users","admin-pricing","admin-finance","admin-roles"];
 bool IsStaff => User.IsInRole("ContentEditor")||User.IsInRole("ContentReviewer")||User.IsInRole("SuperAdmin");
 [HttpGet("login")] public IActionResult Login()=>View("Auth",new EduPage{Page="login"});
 [HttpGet("register")] public IActionResult Register()=>View("Auth",new EduPage{Page="register"});
 [HttpGet("admin-login")] public IActionResult AdminLogin()=>Redirect("/edu/login");
 [HttpPost("register"),EnableRateLimiting("auth")]
 public async Task<IActionResult> Register(EduRegistration form) {
  if(form.Semester is <1 or >20) ModelState.AddModelError("Semester","Kỳ học không hợp lệ.");
  if(string.IsNullOrWhiteSpace(form.FullName)) ModelState.AddModelError("FullName","Nhập họ tên.");
  if(!ModelState.IsValid) return View("Auth",new EduPage{Page="register"});
  try {
   var id=await store.Register(form,new PasswordHasher<string>().HashPassword(form.Email,form.Password));
   await SignIn(id,form.FullName,false); return Redirect("/edu/onboarding");
  } catch(SqlException e) when(e.Number is 2601 or 2627) { ModelState.AddModelError("","Email đã được sử dụng."); return View("Auth",new EduPage{Page="register"}); }
 }
 [HttpPost("login"),EnableRateLimiting("auth")]
 public async Task<IActionResult> Login(EduLogin form) {
  if(ModelState.IsValid) {
   var row=(await store.Query("SELECT UserId,FullName,PasswordHash FROM dbo.Users WHERE NormalizedEmail=UPPER(LTRIM(RTRIM(@e))) AND Status='Active'",("@e",form.Email))).FirstOrDefault();
   if(row!=null){var roles=await store.Query("SELECT r.Code FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UserId=@u AND r.Code IN('ContentEditor','ContentReviewer','SuperAdmin')",("@u",EduPage.Number(row,"UserId")));if(roles.Count>0)return await StaffLogin(form);}
   bool valid=false;
   if(row!=null && EduPage.Text(row,"PasswordHash").Length>0) try { valid=new PasswordHasher<string>().VerifyHashedPassword(form.Email,EduPage.Text(row,"PasswordHash"),form.Password)!=PasswordVerificationResult.Failed; } catch(FormatException) { }
   if(valid) {
    await SignIn(EduPage.Number(row!,"UserId"),EduPage.Text(row!,"FullName"),form.Remember); return Redirect("/edu");
   }
  }
  ModelState.AddModelError("","Email hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa."); return View("Auth",new EduPage{Page="login"});
 }
 [HttpPost("admin-login"),EnableRateLimiting("auth")]
 public Task<IActionResult> AdminLogin(EduLogin form)=>Login(form);
 async Task<IActionResult> StaffLogin(EduLogin form) {
  var row=(await store.Query("SELECT u.UserId,u.FullName,u.PasswordHash,s.LockedUntil FROM dbo.Users u LEFT JOIN dbo.AdminSecurity s ON s.UserId=u.UserId WHERE u.NormalizedEmail=UPPER(LTRIM(RTRIM(@e))) AND u.Status='Active' AND EXISTS(SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UserId=u.UserId AND r.Code IN('ContentEditor','ContentReviewer','SuperAdmin'))",("@e",form.Email??""))).FirstOrDefault();
  bool valid=false;
  if(row!=null) {
   await store.Execute("INSERT dbo.AdminSecurity(UserId) SELECT @u WHERE NOT EXISTS(SELECT 1 FROM dbo.AdminSecurity WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u)",("@u",EduPage.Number(row,"UserId")));
   if(ModelState.IsValid&&(row["LockedUntil"]==null||Convert.ToDateTime(row["LockedUntil"])<=DateTime.UtcNow))
    try {valid=new PasswordHasher<string>().VerifyHashedPassword(form.Email!,EduPage.Text(row,"PasswordHash"),form.Password)!=PasswordVerificationResult.Failed;}catch(FormatException){ }
   if(valid) {
    var changed=await store.Execute("UPDATE dbo.AdminSecurity SET FailedAttempts=0,LockedUntil=NULL WHERE UserId=@u AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())",("@u",EduPage.Number(row,"UserId")));
    if(changed==1){await SignIn(EduPage.Number(row,"UserId"),EduPage.Text(row,"FullName"),form.Remember);await store.Execute("INSERT dbo.AdminAudit(ActorUserId,Action) VALUES(@u,'AdminLogin')",("@u",EduPage.Number(row,"UserId")));return Redirect("/edu/admin");}
   }
   await store.Execute("UPDATE dbo.AdminSecurity SET FailedAttempts=CASE WHEN FailedAttempts>=4 THEN 0 ELSE FailedAttempts+1 END,LockedUntil=CASE WHEN FailedAttempts>=4 THEN DATEADD(minute,15,SYSUTCDATETIME()) ELSE LockedUntil END WHERE UserId=@u AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())",("@u",EduPage.Number(row,"UserId")));
  }
  ModelState.AddModelError("","Email hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa. Sai 5 lần sẽ khóa 15 phút.");return View("Auth",new EduPage{Page="login"});
 }
 async Task SignIn(long id,string name,bool remember) {
  var roles=await store.Query("SELECT r.Code FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UserId=@u",("@u",id));
  var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,id.ToString()),new(ClaimTypes.Name,name)};
  claims.AddRange(roles.Select(r=>new Claim(ClaimTypes.Role,EduPage.Text(r,"Code"))));
  await HttpContext.SignInAsync(Scheme,new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme)),new AuthenticationProperties{IsPersistent=remember,ExpiresUtc=DateTimeOffset.UtcNow.AddDays(remember?7:1)});
 }
 [HttpPost("logout"),Authorize(AuthenticationSchemes=Scheme)] public async Task<IActionResult> Logout(){ await HttpContext.SignOutAsync(Scheme);return Redirect("/edu/login"); }
 [HttpGet("preview/{page?}")]
 public async Task<IActionResult> Preview(string page="today",long id=0,string? q=null) {
  if(!Pages.Contains(page))return NotFound();
  if(page=="pricing")return Redirect("/edu/"+(Request.Path.Value!.Contains("/preview/")?"preview/":"")+"wallet#coin-packages"); var vm=await store.Load(page,0,true,id,q); vm.IsAdmin=page.StartsWith("admin");
  if(vm.IsAdmin) await LoadAdmin(vm,true); return View("Page",vm);
 }
 [HttpGet("{page?}"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Page(string page="today",long id=0,string? q=null) {
  if(!Pages.Contains(page))return NotFound();
  if(page=="pricing")return Redirect("/edu/"+(Request.Path.Value!.Contains("/preview/")?"preview/":"")+"wallet#coin-packages");
  if(page.StartsWith("admin")&&!IsStaff)return Forbid(Scheme);
  if(page is "admin-users" or "admin-pricing" or "admin-finance" or "admin-roles" && !User.IsInRole("SuperAdmin"))return Forbid(Scheme);
  var vm=await store.Load(page,Uid,false,id,q); vm.IsAdmin=page.StartsWith("admin");
  if(vm.IsAdmin) await LoadAdmin(vm,false); return View("Page",vm);
 }
 async Task LoadAdmin(EduPage vm,bool preview) {
  if(preview)return; // Public previews never read operational/user records.
  vm.Items=vm.Page switch {
   "admin-reports"=>await store.Query("SELECT r.*,CASE WHEN i.OwnerUserId IS NULL THEN i.Title ELSE N'Nội dung riêng (ẩn)' END Title FROM dbo.ContentReports r LEFT JOIN dbo.ContentItems i ON i.ContentId=r.ContentId ORDER BY r.DueAt"),
   "admin-review"=>await store.Query("SELECT i.*,c.QualityScore,c.Premise FROM dbo.ContentItems i LEFT JOIN dbo.Cases c ON c.ContentId=i.ContentId WHERE i.OwnerUserId IS NULL AND i.Status IN('Review','Draft') ORDER BY i.CreatedAt"),
   "admin-slides"=>await store.Query("SELECT ch.*,d.OriginalName,v.SlideVersionId,v.VersionNumber,v.Status FROM dbo.Chapters ch LEFT JOIN dbo.SlideVersions v ON v.ChapterId=ch.ChapterId LEFT JOIN dbo.Documents d ON d.DocumentId=v.DocumentId"),
   "admin-cases"=>await store.Query("SELECT i.*,c.CompanyName,c.QualityScore,c.IsHypothetical FROM dbo.Cases c JOIN dbo.ContentItems i ON i.ContentId=c.ContentId WHERE i.OwnerUserId IS NULL"),
   "admin-gaps"=>await store.Query("SELECT * FROM dbo.ContentGaps ORDER BY CreatedAt DESC"),
   "admin-users"=>await store.Query("SELECT u.UserId,u.FullName,u.Email,u.Status,w.Balance FROM dbo.Users u JOIN dbo.Wallets w ON w.UserId=u.UserId ORDER BY u.UserId DESC"),
   "admin-pricing"=>await store.Query("SELECT * FROM dbo.CoinPackages ORDER BY PriceVnd"),
   "admin-finance"=>await store.Query("SELECT FeatureCode,COUNT(*) Calls,SUM(CostUsd) CostUsd FROM dbo.AiCalls GROUP BY FeatureCode"),
   "admin-roles"=>await store.Query("SELECT r.Code,COUNT(ur.UserId) Members FROM dbo.Roles r LEFT JOIN dbo.UserRoles ur ON ur.RoleId=r.RoleId GROUP BY r.Code"),
   _=>await store.Query("SELECT TOP 10 * FROM dbo.ContentGaps WHERE Status IN('Open','Assigned') ORDER BY DueAt")
  };
  vm.Data=(await store.Query("SELECT (SELECT COUNT(*) FROM dbo.Users WHERE Status='Active') Users,(SELECT COUNT(*) FROM dbo.ContentItems WHERE Status='Review' AND OwnerUserId IS NULL) Reviews,(SELECT COUNT(*) FROM dbo.ContentReports WHERE Status IN('Open','Investigating')) Reports,(SELECT COUNT(*) FROM dbo.ContentGaps WHERE Status IN('Open','Assigned')) Gaps")).First();
 }
 [HttpPost("onboarding"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Onboarding(int[] courseIds,int chapter=4,string[]? weekdays=null,DateTime? examDate=null) {
  if(courseIds.Length==0){TempData["EduError"]="Chọn ít nhất một môn học.";return Redirect("/edu/onboarding");}
  foreach(var cid in courseIds.Distinct()) {
   await store.Execute("""
    SET XACT_ABORT ON; BEGIN TRAN;
    IF EXISTS(SELECT 1 FROM dbo.Courses WHERE CourseId=@c AND IsAvailable=1) AND NOT EXISTS(SELECT 1 FROM dbo.Enrollments WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND CourseId=@c)
    BEGIN
     INSERT dbo.Enrollments(UserId,CourseId,CurrentChapterId) VALUES(@u,@c,(SELECT ChapterId FROM dbo.Chapters WHERE CourseId=@c AND ChapterNumber=@ch));
     DECLARE @e bigint=SCOPE_IDENTITY();
     INSERT dbo.Exams(EnrollmentId,Kind,Name,ExamDate) VALUES(@e,'Midterm',N'Thi giữa kỳ',@d);
    END;
    UPDATE dbo.Users SET OnboardingStep=3 WHERE UserId=@u; COMMIT;
    """,("@u",Uid),("@c",cid),("@ch",chapter),("@d",examDate));
   if(weekdays!=null) foreach(var day in weekdays) if(int.TryParse(day,out var n)&&n is >=1 and <=7)
    await store.Execute("INSERT dbo.ClassMeetings(EnrollmentId,IsoWeekday) SELECT e.EnrollmentId,@d FROM dbo.Enrollments e WHERE e.UserId=@u AND e.CourseId=@c AND NOT EXISTS(SELECT 1 FROM dbo.ClassMeetings cm WHERE cm.EnrollmentId=e.EnrollmentId AND cm.IsoWeekday=@d)",("@d",n),("@u",Uid),("@c",cid));
  }
  TempData["EduNotice"]="Đã lưu môn và lịch học. 20 coin chào mừng đã có trong ví.";return Redirect("/edu");
 }
 [HttpGet("api/question"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Question(long id) {
  var q=(await store.Query("SELECT q.ContentId,q.Prompt FROM dbo.Questions q JOIN dbo.ContentItems i ON i.ContentId=q.ContentId WHERE q.ContentId=@id AND i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u)",("@id",id),("@u",Uid))).FirstOrDefault();
  if(q==null)return NotFound();var opts=await store.Query("SELECT OptionId,Position,Text FROM dbo.QuestionOptions WHERE QuestionId=@id ORDER BY Position",("@id",id));return Json(new{question=q,options=opts});
 }
 [HttpPost("api/answer"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Answer(long questionId,long optionId,string mode="quiz") {
  var q=(await store.Query("SELECT q.*,o.IsCorrect,i.ChapterId FROM dbo.Questions q JOIN dbo.QuestionOptions o ON o.QuestionId=q.ContentId JOIN dbo.ContentItems i ON i.ContentId=q.ContentId WHERE q.ContentId=@q AND o.OptionId=@o AND i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u)",("@q",questionId),("@o",optionId),("@u",Uid))).FirstOrDefault();
  if(q==null)return BadRequest();bool correct=Convert.ToBoolean(q["IsCorrect"]);
  await store.Execute("""
   SET XACT_ABORT ON; BEGIN TRAN;
   INSERT dbo.StudySessions(UserId,Kind,Status,CompletedAt,Score,PossibleScore) VALUES(@u,@kind,'Completed',SYSUTCDATETIME(),@score,1);
   DECLARE @s bigint=SCOPE_IDENTITY();
   INSERT dbo.SessionItems(SessionId,ContentId,Position) VALUES(@s,@q,1); DECLARE @item bigint=SCOPE_IDENTITY();
   INSERT dbo.QuestionAnswers(SessionItemId,QuestionId,SelectedOptionId,IsCorrect) VALUES(@item,@q,@o,@correct);
   IF @correct=0 BEGIN
    UPDATE dbo.WrongQuestions WITH(UPDLOCK,HOLDLOCK) SET WrongCount=WrongCount+1,LastWrongAt=SYSUTCDATETIME(),ResolvedAt=NULL WHERE UserId=@u AND QuestionId=@q;
    IF @@ROWCOUNT=0 INSERT dbo.WrongQuestions(UserId,QuestionId) VALUES(@u,@q);
   END ELSE UPDATE dbo.WrongQuestions SET ResolvedAt=SYSUTCDATETIME() WHERE UserId=@u AND QuestionId=@q;
   MERGE dbo.UserConceptProgress WITH(HOLDLOCK) AS t USING(SELECT ConceptId FROM dbo.ContentConcepts WHERE ContentId=@q) AS s
   ON t.UserId=@u AND t.ConceptId=s.ConceptId
   WHEN MATCHED THEN UPDATE SET CorrectCount=CorrectCount+@score,WrongCount=WrongCount+1-@score,Mastery=100.0*(CorrectCount+@score)/(CorrectCount+WrongCount+1),UpdatedAt=SYSUTCDATETIME()
   WHEN NOT MATCHED THEN INSERT(UserId,ConceptId,CorrectCount,WrongCount,Mastery) VALUES(@u,s.ConceptId,@score,1-@score,100*@score);
   DECLARE @today date=CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME()));
   UPDATE dbo.DailyActivity WITH(UPDLOCK,HOLDLOCK) SET CompletedSessions=CompletedSessions+1 WHERE UserId=@u AND LocalDate=@today;
   IF @@ROWCOUNT=0 INSERT dbo.DailyActivity(UserId,LocalDate,CompletedSessions) VALUES(@u,@today,1);
   COMMIT;
   """,("@u",Uid),("@q",questionId),("@o",optionId),("@correct",correct),("@score",correct?1:0),("@kind",mode=="links"?"ConceptLink":"Quiz"));
  var sources=await store.Query("SELECT SlideId FROM dbo.ContentSources WHERE ContentId=@q",("@q",questionId));
  var correctOptions=await store.Query("SELECT Text FROM dbo.QuestionOptions WHERE QuestionId=@q AND IsCorrect=1 ORDER BY Position",("@q",questionId));
  return Json(new{correct,correctAnswer=string.Join(" · ",correctOptions.Select(x=>EduPage.Text(x,"Text"))),explanation=EduPage.Text(q,"Explanation"),slideId=sources.FirstOrDefault() is {} s?EduPage.Number(s,"SlideId"):0});
 }
 [HttpPost("api/review"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Review(long contentId,bool remembered) {
  await store.Execute("""
   SET XACT_ABORT ON; BEGIN TRAN;
   IF NOT EXISTS(SELECT 1 FROM dbo.Flashcards f JOIN dbo.ContentItems i ON i.ContentId=f.ContentId WHERE f.ContentId=@c AND i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u)) THROW 51100,'Card not available',1;
   DECLARE @days int=0;
   SELECT @days=IntervalDays FROM dbo.ReviewSchedules WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND FlashcardId=@c;
   SET @days=CASE WHEN @r=0 THEN 0 WHEN @days<1 THEN 1 WHEN @days<3 THEN 3 WHEN @days<7 THEN 7 ELSE 14 END;
   DECLARE @due datetime2=CASE WHEN @r=0 THEN DATEADD(minute,10,SYSUTCDATETIME()) ELSE DATEADD(day,@days,SYSUTCDATETIME()) END;
   UPDATE dbo.ReviewSchedules SET IntervalDays=@days,Repetitions=Repetitions+1,NextDueAt=@due,LastReviewedAt=SYSUTCDATETIME() WHERE UserId=@u AND FlashcardId=@c;
   IF @@ROWCOUNT=0 INSERT dbo.ReviewSchedules(UserId,FlashcardId,IntervalDays,Repetitions,NextDueAt,LastReviewedAt) VALUES(@u,@c,@days,1,@due,SYSUTCDATETIME());
   INSERT dbo.StudySessions(UserId,Kind,Status,CompletedAt) VALUES(@u,'Flashcard','Completed',SYSUTCDATETIME()); DECLARE @s bigint=SCOPE_IDENTITY();
   INSERT dbo.SessionItems(SessionId,ContentId,Position) VALUES(@s,@c,1); DECLARE @si bigint=SCOPE_IDENTITY();
   INSERT dbo.FlashcardReviews(SessionItemId,Remembered,NextDueAt) VALUES(@si,@r,@due);
   COMMIT;
   """,("@u",Uid),("@c",contentId),("@r",remembered));return Json(new{ok=true});
 }
 [HttpPost("api/case-choice"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> CaseChoice(long optionId,long runId=0) {
  var row=(await store.Query("SELECT o.*,s.CaseId,s.StepNumber FROM dbo.CaseOptions o JOIN dbo.CaseSteps s ON s.CaseStepId=o.CaseStepId JOIN dbo.ContentItems i ON i.ContentId=s.CaseId WHERE o.CaseOptionId=@o AND i.Status='Published' AND (i.OwnerUserId IS NULL OR i.OwnerUserId=@u)",("@o",optionId),("@u",Uid))).FirstOrDefault();
  if(row==null)return BadRequest();
  var caseId=EduPage.Number(row,"CaseId");var step=EduPage.Number(row,"CaseStepId");var stepNumber=EduPage.Number(row,"StepNumber");
  await using var c=store.Connection();await c.OpenAsync();await using var tx=(SqlTransaction)await c.BeginTransactionAsync();
  if(runId==0) {
   if(stepNumber!=1)return BadRequest();
   await using var start=EduStore.Command(c,"INSERT dbo.StudySessions(UserId,Kind) VALUES(@u,'Case'); DECLARE @s bigint=SCOPE_IDENTITY(); INSERT dbo.CaseRuns(SessionId,CaseId) VALUES(@s,@c);SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@u",Uid),("@c",caseId)); start.Transaction=tx;runId=Convert.ToInt64(await start.ExecuteScalarAsync());
  }
  await using var cmd=EduStore.Command(c,"""
   DECLARE @s bigint;
   SELECT @s=r.SessionId FROM dbo.CaseRuns r WITH(UPDLOCK,HOLDLOCK) JOIN dbo.StudySessions ss ON ss.SessionId=r.SessionId WHERE r.CaseRunId=@run AND r.CaseId=@c AND ss.UserId=@u AND ss.Status='Active';
   IF @s IS NULL THROW 51101,'Run not available',1;
   IF (SELECT COUNT(*) FROM dbo.CaseChoices WHERE CaseRunId=@run)<>@stepNumber-1 THROW 51102,'Step already answered or out of order',1;
   INSERT dbo.CaseChoices(CaseRunId,CaseId,CaseStepId,CaseOptionId) VALUES(@run,@c,@step,@option);
   UPDATE dbo.CaseRuns SET StepsPassed=StepsPassed+@passed WHERE CaseRunId=@run;
   IF @stepNumber=4 BEGIN
    UPDATE dbo.StudySessions SET Status='Completed',CompletedAt=SYSUTCDATETIME(),Score=(SELECT StepsPassed FROM dbo.CaseRuns WHERE CaseRunId=@run),PossibleScore=4 WHERE SessionId=@s;
    INSERT dbo.SessionItems(SessionId,ContentId,Position) VALUES(@s,@c,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.CaseCollection WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND CaseId=@c) BEGIN
     INSERT dbo.CaseCollection(UserId,CaseId) VALUES(@u,@c);
     DECLARE @key varchar(150)=CONCAT('first-case:',@u,':',@c);
     EXEC dbo.ApplyCoinChange @u,1,'FirstCase',@key;
     INSERT dbo.RewardClaims(UserId,RewardCode,ScopeKey,LedgerId) SELECT @u,'FirstCase',CONVERT(varchar(100),@c),LedgerId FROM dbo.CoinLedger WHERE IdempotencyKey=@key;
    END;
   END;
   """,("@run",runId),("@c",caseId),("@u",Uid),("@step",step),("@stepNumber",stepNumber),("@option",optionId),("@passed",Convert.ToDecimal(row["Quality"])>=0.6m?1:0));
  cmd.Transaction=tx;await cmd.ExecuteNonQueryAsync();await tx.CommitAsync();return Json(new{runId,feedback=EduPage.Text(row,"Feedback"),complete=stepNumber==4});
 }
 [HttpPost("note"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Note(long slideId,string note) {
  await store.Execute("""
   SET XACT_ABORT ON; BEGIN TRAN;
   IF EXISTS(SELECT 1 FROM dbo.Slides s JOIN dbo.Documents d ON d.DocumentId=s.DocumentId WHERE s.SlideId=@s AND (d.Scope='Library' OR d.OwnerUserId=@u)) BEGIN
    UPDATE dbo.SlideNotes WITH(UPDLOCK,HOLDLOCK) SET Note=@n,UpdatedAt=SYSUTCDATETIME() WHERE UserId=@u AND SlideId=@s;
    IF @@ROWCOUNT=0 INSERT dbo.SlideNotes(UserId,SlideId,Note) VALUES(@u,@s,@n);
   END; COMMIT;
   """,("@u",Uid),("@s",slideId),("@n",note??""));TempData["EduNotice"]="Đã lưu ghi chú riêng.";return Redirect("/edu/slides?id="+slideId);
 }
 [HttpPost("report"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Report(long contentId,string reason,string? details,string? returnUrl=null) {
  if(reason is not("WrongGrading" or "ExplanationMismatch" or "MissingSource" or "Unclear" or "Other"))return BadRequest();
  if(contentId<=0||(details?.Length??0)>2000)return BadRequest();
  var inserted=await store.Execute("INSERT dbo.ContentReports(ReporterUserId,ContentId,Reason,Details) SELECT @u,ContentId,@r,@d FROM dbo.ContentItems WHERE ContentId=@c AND (OwnerUserId IS NULL OR OwnerUserId=@u)",("@u",Uid),("@c",contentId),("@r",reason),("@d",details));if(inserted==0)return NotFound();TempData["EduNotice"]="Đã gửi báo cáo AI. Đội nội dung sẽ kiểm tra trong 48 giờ.";return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/edu/vault");
 }
 [HttpPost("settings"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Settings(bool dailyReminder,bool afterClassReminder,bool beforeExamReminder,bool slideUpdateReminder,bool memoryEnabled,string reminderTime="21:00") {
  if(!TimeOnly.TryParse(reminderTime,out var time))return BadRequest();
  await store.Execute("UPDATE dbo.UserSettings SET DailyReminder=@a,AfterClassReminder=@b,BeforeExamReminder=@c,SlideUpdateReminder=@d,MemoryEnabled=@e,ReminderTime=@t WHERE UserId=@u",("@a",dailyReminder),("@b",afterClassReminder),("@c",beforeExamReminder),("@d",slideUpdateReminder),("@e",memoryEnabled),("@t",time.ToTimeSpan()),("@u",Uid));TempData["EduNotice"]="Đã lưu cài đặt.";return Redirect("/edu/settings");
 }
 [HttpPost("waitlist"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Waitlist(string feature) {
  await store.Execute("INSERT dbo.FeatureWaitlist(UserId,FeatureCode) SELECT @u,FeatureCode FROM dbo.FeaturePolicies p WHERE FeatureCode=@f AND Availability='ComingSoon' AND NOT EXISTS(SELECT 1 FROM dbo.FeatureWaitlist WHERE UserId=@u AND FeatureCode=p.FeatureCode)",("@u",Uid),("@f",feature));TempData["EduNotice"]="Đã ghi nhận đăng ký nhận thông báo khi mở.";return Redirect("/edu/tools");
 }
 [HttpPost("topup"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Topup(int packageId,string provider) {
  if(provider is not("MoMo" or "ZaloPay" or "VietQR" or "Card"))return BadRequest();
  // Do not create a misleading payable order before a gateway is configured.
  TempData["EduError"]="Cổng thanh toán chưa được cấu hình. Chưa thu tiền hoặc thay đổi số dư coin.";await Task.CompletedTask;return Redirect("/edu/topup");
 }
 [HttpPost("upload"),Authorize(AuthenticationSchemes=Scheme),RequestSizeLimit(22000000)]
 public async Task<IActionResult> Upload(IFormFile file) {
  if(file==null||file.Length is <=0 or >20971520||!file.FileName.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)){TempData["EduError"]="Chọn file PDF không quá 20 MB.";return Redirect("/edu/files");}
  var dir=Path.Combine(env.ContentRootPath,"App_Data","edu-private");Directory.CreateDirectory(dir);var key=Guid.NewGuid().ToString("N")+".pdf";var path=Path.Combine(dir,key);
  try {
   await using(var f=System.IO.File.Create(path))await file.CopyToAsync(f);
   var bytes=await System.IO.File.ReadAllBytesAsync(path);if(bytes.Length<5||System.Text.Encoding.ASCII.GetString(bytes,0,5)!="%PDF-")throw new InvalidDataException();
   using var pdf=PdfDocument.Open(path);if(pdf.NumberOfPages>40)throw new InvalidDataException();
   await using var c=store.Connection();await c.OpenAsync();await using var tx=(SqlTransaction)await c.BeginTransactionAsync();
   await using var cmd=EduStore.Command(c,"INSERT dbo.Documents(OwnerUserId,Scope,Kind,OriginalName,StorageKey,ByteLength,PageCount,Sha256) VALUES(@u,'Private','Slides',@n,@k,@b,@p,@hash);SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@u",Uid),("@n",Path.GetFileName(file.FileName)),("@k",key),("@b",file.Length),("@p",pdf.NumberOfPages),("@hash",SHA256.HashData(bytes)));cmd.Transaction=tx;var doc=Convert.ToInt64(await cmd.ExecuteScalarAsync());
   foreach(var p in pdf.GetPages().Take(15)) { await using var s=EduStore.Command(c,"INSERT dbo.Slides(DocumentId,PageNumber,ExtractedText) VALUES(@d,@p,@t)",("@d",doc),("@p",p.Number),("@t",p.Text));s.Transaction=tx;await s.ExecuteNonQueryAsync(); }
   await tx.CommitAsync();TempData["EduNotice"]="Đã tải PDF và trích xuất tối đa 15 trang miễn phí. Nội dung riêng chỉ tài khoản của bạn xem được.";
  }catch(Exception ex) when(ex is not OperationCanceledException) {if(System.IO.File.Exists(path))System.IO.File.Delete(path);TempData["EduError"]="Chưa đọc được file. PDF cần không có mật khẩu, tối đa 40 trang. Bạn chưa bị trừ coin.";}
  return Redirect("/edu/files");
 }
 [HttpPost("generate-study"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> GenerateStudy(long id,CancellationToken ct) {
  var doc=(await store.Query("SELECT * FROM dbo.Documents WHERE DocumentId=@d AND OwnerUserId=@u AND DeletedAt IS NULL",("@d",id),("@u",Uid))).FirstOrDefault();if(doc==null)return NotFound();
  var rows=await store.Query("SELECT SlideId,PageNumber,ExtractedText FROM dbo.Slides WHERE DocumentId=@d AND LEN(ExtractedText)>10 ORDER BY PageNumber",("@d",id));
  if(rows.Count==0){TempData["EduError"]="PDF chưa có chữ trích xuất được. File scan cần OCR trước khi tải.";return Redirect("/edu/files");}
  try {
   var sources=rows.Select(r=>new Slide{SlideNumber=(int)EduPage.Number(r,"PageNumber"),SlideText=EduPage.Text(r,"ExtractedText")}).ToList();
   var quiz=await ai.Practice(sources,"Nội dung chính của tài liệu","",Math.Min(6,rows.Count),ct);
   await using var c=store.Connection();await c.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
   await using(var charge=EduStore.Command(c,"""
    IF @live=1 BEGIN
     DECLARE @day date=CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME())),@used int=0,@price int;
     SELECT @used=UsedUnits FROM dbo.UsageCounters WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND FeatureCode='DeepPractice' AND ScopeKey='all' AND PeriodStart=@day;
     SET @price=CEILING(CONVERT(decimal(10,2),CASE WHEN @used>=6 THEN @count WHEN @used+@count<=6 THEN 0 ELSE @used+@count-6 END)/3);
     IF @price>0 BEGIN DECLARE @key varchar(150)=CONCAT('generate-study:',NEWID()),@amount int=-@price;EXEC dbo.ApplyCoinChange @u,@amount,'DeepPractice',@key;END;
     UPDATE dbo.UsageCounters SET UsedUnits=UsedUnits+@count WHERE UserId=@u AND FeatureCode='DeepPractice' AND ScopeKey='all' AND PeriodStart=@day;
     IF @@ROWCOUNT=0 INSERT dbo.UsageCounters(UserId,FeatureCode,ScopeKey,PeriodStart,UsedUnits) VALUES(@u,'DeepPractice','all',@day,@count);
    END;
    """,("@live",!ai.IsDemo),("@u",Uid),("@count",quiz.Questions.Count))){charge.Transaction=tx;await charge.ExecuteNonQueryAsync(ct);}
   foreach(var q in quiz.Questions) {
    var source=rows.First(r=>EduPage.Number(r,"PageNumber")==q.SourceSlide);var title=(ai.IsDemo?"Mẫu tra cứu nguồn · ":"")+q.Question;title=title[..Math.Min(title.Length,250)];
    await using var insert=EduStore.Command(c,"""
     INSERT dbo.ContentItems(OwnerUserId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@u,@doc,'Question',@title,'Published',SYSUTCDATETIME()); DECLARE @q bigint=SCOPE_IDENTITY();
     INSERT dbo.Questions(ContentId,Format,Prompt,Explanation) VALUES(@q,'MultipleChoice',@prompt,@why);
     INSERT dbo.QuestionOptions(QuestionId,Position,Text,IsCorrect) VALUES(@q,1,@a,CASE WHEN @correct=0 THEN 1 ELSE 0 END),(@q,2,@b,CASE WHEN @correct=1 THEN 1 ELSE 0 END),(@q,3,@c,CASE WHEN @correct=2 THEN 1 ELSE 0 END);
     INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@q,@slide);
     INSERT dbo.ContentItems(OwnerUserId,SourceDocumentId,Kind,Title,Status,PublishedAt) VALUES(@u,@doc,'Flashcard',@front,'Published',SYSUTCDATETIME());DECLARE @f bigint=SCOPE_IDENTITY();
     INSERT dbo.Flashcards(ContentId,Front,Back) VALUES(@f,@front,@back);INSERT dbo.ContentSources(ContentId,SlideId) VALUES(@f,@slide);
     """,("@u",Uid),("@doc",id),("@title",title),("@prompt",q.Question),("@why",q.Why),("@a",q.Options[0]),("@b",q.Options[1]),("@c",q.Options[2]),("@correct",q.Correct),("@slide",EduPage.Number(source,"SlideId")),("@front","Nội dung trang "+q.SourceSlide+" · "+EduPage.Text(doc,"OriginalName")[..Math.Min(EduPage.Text(doc,"OriginalName").Length,180)]),("@back",EduPage.Text(source,"ExtractedText")));insert.Transaction=tx;await insert.ExecuteNonQueryAsync(ct);
   }
   await using(var log=EduStore.Command(c,"INSERT dbo.AiCalls(UserId,FeatureCode,Provider,Model,InputTokens,OutputTokens,CostUsd,IsSuccess) VALUES(@u,'DeepPractice',@p,@m,@i,@o,@c,1)",("@u",Uid),("@p",ai.Provider),("@m",ai.Model),("@i",ai.InputTokens),("@o",ai.OutputTokens),("@c",ai.Cost))){log.Transaction=tx;await log.ExecuteNonQueryAsync(ct);}
   await tx.CommitAsync(ct);TempData["EduNotice"]=ai.IsDemo?"Đã tạo thẻ trích đoạn và câu hỏi mẫu tra cứu từ PDF của bạn. Không gọi AI, không thu coin.":"Đã tạo bộ câu hỏi riêng có liên kết slide nguồn.";return Redirect("/edu/quiz");
  }catch(Exception ex) when(ex is not OperationCanceledException){TempData["EduError"]="Chưa tạo được bộ học. Kiểm tra cấu hình AI hoặc số dư coin; không có khoản trừ coin được ghi nhận khi tạo thất bại.";return Redirect("/edu/files");}
 }
 [HttpGet("document/{id:long}"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> Document(long id) {
  var doc=(await store.Query("SELECT StorageKey,OriginalName FROM dbo.Documents WHERE DocumentId=@d AND OwnerUserId=@u AND DeletedAt IS NULL",("@d",id),("@u",Uid))).FirstOrDefault();
  if(doc==null)return NotFound();var path=Path.Combine(env.ContentRootPath,"App_Data","edu-private",Path.GetFileName(EduPage.Text(doc,"StorageKey")));if(!System.IO.File.Exists(path))return NotFound();return PhysicalFile(path,"application/pdf",enableRangeProcessing:true);
 }
 [HttpPost("delete-file"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> DeleteFile(long id) {await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;UPDATE dbo.Documents SET DeletedAt=SYSUTCDATETIME() WHERE DocumentId=@d AND OwnerUserId=@u;UPDATE dbo.ContentItems SET Status='Hidden' WHERE SourceDocumentId=@d AND OwnerUserId=@u;COMMIT;",("@d",id),("@u",Uid));return Redirect("/edu/files");}
 [HttpPost("unlock-pdf"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> UnlockPdf(long id) {
  var doc=(await store.Query("SELECT * FROM dbo.Documents WHERE DocumentId=@id AND OwnerUserId=@u AND DeletedAt IS NULL",("@id",id),("@u",Uid))).FirstOrDefault();if(doc==null)return NotFound();
  if(EduPage.Number(doc,"PageCount")<=15)return Redirect("/edu/files");
  var path=Path.Combine(env.ContentRootPath,"App_Data","edu-private",Path.GetFileName(EduPage.Text(doc,"StorageKey")));if(!System.IO.File.Exists(path))return NotFound();
  try {
   using var pdf=PdfDocument.Open(path);await using var c=store.Connection();await c.OpenAsync();await using var tx=(SqlTransaction)await c.BeginTransactionAsync();
   await using var charge=EduStore.Command(c,"DECLARE @key varchar(150)=CONCAT('pdf-unlock:',@id);EXEC dbo.ApplyCoinChange @u, -15,'UnlockPdf',@key;UPDATE dbo.Documents SET UnlockedPages=PageCount WHERE DocumentId=@id AND OwnerUserId=@u;",("@u",Uid),("@id",id));charge.Transaction=tx;await charge.ExecuteNonQueryAsync();
   foreach(var pg in pdf.GetPages().Skip(15)){await using var cmd=EduStore.Command(c,"IF NOT EXISTS(SELECT 1 FROM dbo.Slides WHERE DocumentId=@d AND PageNumber=@p) INSERT dbo.Slides(DocumentId,PageNumber,ExtractedText) VALUES(@d,@p,@t)",("@d",id),("@p",pg.Number),("@t",pg.Text));cmd.Transaction=tx;await cmd.ExecuteNonQueryAsync();}await tx.CommitAsync();TempData["EduNotice"]="Đã mở các trang còn lại. Trừ 15 coin một lần cho tài liệu này.";
  }catch(SqlException ex)when(ex.Number==51005){TempData["EduError"]="Bạn chưa đủ 15 coin để mở tài liệu.";}return Redirect("/edu/files");
 }
 [HttpPost("save-case"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> SaveCase(long runId) {
  await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;IF EXISTS(SELECT 1 FROM dbo.CaseRuns r JOIN dbo.StudySessions s ON s.SessionId=r.SessionId WHERE r.CaseRunId=@r AND s.UserId=@u AND s.Status='Completed') AND NOT EXISTS(SELECT 1 FROM dbo.VaultSlots WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND CaseRunId=@r) BEGIN UPDATE dbo.VaultSlots SET CaseRunId=@r WHERE UserId=@u AND SlotNumber=(SELECT MIN(SlotNumber) FROM dbo.VaultSlots WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u AND CaseRunId IS NULL);END;COMMIT;",("@r",runId),("@u",Uid));TempData["EduNotice"]="Đã lưu nếu còn ô trống. Mỗi kết quả chỉ lưu một lần.";return Redirect("/edu/vault");
 }
 [HttpPost("buy-slot"),Authorize(AuthenticationSchemes=Scheme)]
 public async Task<IActionResult> BuySlot() {
  try{await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;DECLARE @n int;SELECT @n=COUNT(*)+1 FROM dbo.VaultSlots WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@u;IF @n>9 THROW 51104,'Maximum slots reached',1;DECLARE @key varchar(150)=CONCAT('vault-slot:',@u,':',@n);EXEC dbo.ApplyCoinChange @u,-10,'VaultSlot',@key;INSERT dbo.VaultSlots(UserId,SlotNumber) VALUES(@u,@n);COMMIT;",("@u",Uid));TempData["EduNotice"]="Đã thêm 1 ô lưu, trừ 10 coin.";}catch(SqlException ex)when(ex.Number is 51005 or 51104){TempData["EduError"]="Không đủ coin hoặc đã đạt tối đa 9 ô lưu.";}return Redirect("/edu/vault");
 }
 [HttpPost("admin-report"),Authorize(AuthenticationSchemes=Scheme,Roles="ContentReviewer,SuperAdmin")]
 public async Task<IActionResult> AdminReport(long id,bool confirmed,string resolution) {
  await store.Execute("""
   SET XACT_ABORT ON;BEGIN TRAN;
   DECLARE @reporter bigint,@content bigint;
   SELECT @reporter=ReporterUserId,@content=ContentId FROM dbo.ContentReports WITH(UPDLOCK,HOLDLOCK) WHERE ReportId=@id AND Status IN('Open','Investigating');
   IF @reporter IS NOT NULL BEGIN
    UPDATE dbo.ContentReports SET Status=CASE WHEN @yes=1 THEN 'Confirmed' ELSE 'Dismissed' END,AssignedTo=@u,ResolvedAt=SYSUTCDATETIME(),Resolution=@text WHERE ReportId=@id;
    IF @yes=1 BEGIN
     UPDATE dbo.ContentItems SET Status='Hidden' WHERE ContentId=@content;
     DECLARE @key varchar(150)=CONCAT('valid-report:',@id);
     EXEC dbo.ApplyCoinChange @reporter,5,'ValidReport',@key;
     INSERT dbo.RewardClaims(UserId,RewardCode,ScopeKey,LedgerId) SELECT @reporter,'ValidReport',CONVERT(varchar(100),@id),LedgerId FROM dbo.CoinLedger WHERE IdempotencyKey=@key;
     UPDATE dbo.ContentReports SET RewardClaimId=SCOPE_IDENTITY() WHERE ReportId=@id;
     UPDATE a SET ExcludedFromReadiness=1 FROM dbo.QuestionAnswers a JOIN dbo.SessionItems si ON si.SessionItemId=a.SessionItemId WHERE si.ContentId=@content;
    END;
    INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'ResolveReport','ContentReport',CONVERT(nvarchar(100),@id));
   END;COMMIT;
   """,("@id",id),("@u",Uid),("@yes",confirmed),("@text",resolution??""));return Redirect("/edu/admin-reports");
 }
 [HttpPost("admin-gap"),Authorize(AuthenticationSchemes=Scheme,Roles="ContentEditor,ContentReviewer,SuperAdmin")]
 public async Task<IActionResult> AdminGap(long id) {await store.Execute("UPDATE dbo.ContentGaps SET AssignedTo=@u,Status='Assigned' WHERE GapId=@id AND Status='Open';INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'ClaimGap','ContentGap',CONVERT(nvarchar(100),@id));",("@id",id),("@u",Uid));return Redirect("/edu/admin-gaps");}
 [HttpPost("admin-user"),Authorize(AuthenticationSchemes=Scheme,Roles="SuperAdmin")]
 public async Task<IActionResult> AdminUser(long id,string operation,int amount=0,string? reason=null) {
  if(operation=="coins") {
   if(amount==0||Math.Abs((long)amount)>1000000||string.IsNullOrWhiteSpace(reason))return BadRequest();
   try{await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;DECLARE @key varchar(150)=CONCAT('admin-adjust:',NEWID()),@ref nvarchar(100)=CONVERT(nvarchar(100),@admin);EXEC dbo.ApplyCoinChange @id,@amount,'AdminAdjustment',@key,'Admin',@ref;INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey,MetadataJson) VALUES(@admin,'AdjustCoins','User',CONVERT(nvarchar(100),@id),(SELECT @amount Amount,@reason Reason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));COMMIT;",("@id",id),("@amount",amount),("@admin",Uid),("@reason",reason));}catch(SqlException e)when(e.Number is 51004 or 51005){TempData["EduError"]="Không thể điều chỉnh: tài khoản khóa hoặc số dư không đủ.";}
  }else if(operation is "lock" or "unlock") {
   if(id==Uid)return BadRequest();await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;UPDATE dbo.Users SET Status=@s WHERE UserId=@id AND Status IN('Active','Locked');INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,@op,'User',CONVERT(nvarchar(100),@id));COMMIT;",("@s",operation=="lock"?"Locked":"Active"),("@id",id),("@u",Uid),("@op",operation));
  }else return BadRequest();return Redirect("/edu/admin-users");
 }
 [HttpPost("admin-price"),Authorize(AuthenticationSchemes=Scheme,Roles="SuperAdmin")]
 public async Task<IActionResult> AdminPrice(int id,int coins,int priceVnd,bool active) {
  if(coins is <1 or >1000000||priceVnd is <1000 or >100000000)return BadRequest();
  await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;UPDATE dbo.CoinPackages SET Coins=@c,PriceVnd=@p,IsActive=@a WHERE PackageId=@id;INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'UpdatePrice','CoinPackage',CONVERT(nvarchar(100),@id));COMMIT;",("@c",coins),("@p",priceVnd),("@a",active),("@id",id),("@u",Uid));return Redirect("/edu/admin-pricing");
 }
 [HttpPost("admin-role"),Authorize(AuthenticationSchemes=Scheme,Roles="SuperAdmin")]
 public async Task<IActionResult> AdminRole(long id,string role) {
  if(id==Uid||role is not("ContentEditor" or "ContentReviewer" or "SuperAdmin"))return BadRequest();
  await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;IF EXISTS(SELECT 1 FROM dbo.Users WHERE UserId=@id AND Status='Active') BEGIN INSERT dbo.AdminSecurity(UserId) SELECT @id WHERE NOT EXISTS(SELECT 1 FROM dbo.AdminSecurity WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@id); DELETE dbo.UserRoles WHERE UserId=@id;INSERT dbo.UserRoles(UserId,RoleId,GrantedBy) SELECT @id,RoleId,@u FROM dbo.Roles WHERE Code=@r;INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey,MetadataJson) VALUES(@u,'ChangeRole','User',CONVERT(nvarchar(100),@id),(SELECT @r Role FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));END;COMMIT;",("@id",id),("@r",role),("@u",Uid));TempData["EduNotice"]="Đã cập nhật vai trò cho tài khoản. Phiên cũ bị thu hồi khi đổi quyền.";return Redirect("/edu/admin-roles");
 }
 [HttpPost("admin-upload"),Authorize(AuthenticationSchemes=Scheme,Roles="ContentEditor,SuperAdmin"),RequestSizeLimit(22000000)]
 public async Task<IActionResult> AdminUpload(IFormFile file,int chapterId) {
  if(file==null||file.Length is <=0 or >20971520||!file.FileName.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase))return BadRequest();
  var dir=Path.Combine(env.ContentRootPath,"App_Data","edu-library");Directory.CreateDirectory(dir);var key=Guid.NewGuid().ToString("N")+".pdf";var path=Path.Combine(dir,key);
  try {
   await using(var f=System.IO.File.Create(path))await file.CopyToAsync(f);using var pdf=PdfDocument.Open(path);if(pdf.NumberOfPages>200)throw new InvalidDataException();
   await using var c=store.Connection();await c.OpenAsync();await using var tx=(SqlTransaction)await c.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
   await using var cmd=EduStore.Command(c,"INSERT dbo.Documents(ChapterId,Scope,Kind,OriginalName,StorageKey,ByteLength,PageCount,UnlockedPages) VALUES(@ch,'Library','Slides',@n,@k,@b,@pages,@pages);DECLARE @d bigint=SCOPE_IDENTITY(),@v int=COALESCE((SELECT MAX(VersionNumber) FROM dbo.SlideVersions WITH(UPDLOCK,HOLDLOCK) WHERE ChapterId=@ch),0)+1;INSERT dbo.SlideVersions(ChapterId,DocumentId,VersionNumber,Status,UploadedBy) VALUES(@ch,@d,@v,'Review',@u);SELECT @d;",("@ch",chapterId),("@n",Path.GetFileName(file.FileName)),("@k",key),("@b",file.Length),("@pages",pdf.NumberOfPages),("@u",Uid));cmd.Transaction=tx;var doc=Convert.ToInt64(await cmd.ExecuteScalarAsync());
   foreach(var pg in pdf.GetPages()){await using var insert=EduStore.Command(c,"INSERT dbo.Slides(DocumentId,PageNumber,ExtractedText) VALUES(@d,@p,@t)",("@d",doc),("@p",pg.Number),("@t",pg.Text));insert.Transaction=tx;await insert.ExecuteNonQueryAsync();}
   await using(var audit=EduStore.Command(c,"INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'UploadSlideVersion','Document',CONVERT(nvarchar(100),@d));",("@u",Uid),("@d",doc))){audit.Transaction=tx;await audit.ExecuteNonQueryAsync();}await tx.CommitAsync();TempData["EduNotice"]="Đã tải phiên bản slide, chờ duyệt. Chưa thay thế bản đang phát hành.";
  }catch(Exception ex)when(ex is not OperationCanceledException){if(System.IO.File.Exists(path))System.IO.File.Delete(path);TempData["EduError"]="Không đọc được PDF hoặc chương chưa hợp lệ. Chưa phát hành phiên bản mới.";}return Redirect("/edu/admin-slides");
 }
 [HttpPost("admin-publish-slides"),Authorize(AuthenticationSchemes=Scheme,Roles="ContentReviewer,SuperAdmin")]
 public async Task<IActionResult> PublishSlides(long id) {
  await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;DECLARE @ch int;SELECT @ch=ChapterId FROM dbo.SlideVersions WITH(UPDLOCK,HOLDLOCK) WHERE SlideVersionId=@id AND Status='Review';IF @ch IS NOT NULL BEGIN UPDATE dbo.SlideVersions SET Status='Superseded' WHERE ChapterId=@ch AND Status='Published';UPDATE dbo.SlideVersions SET Status='Published',PublishedAt=SYSUTCDATETIME() WHERE SlideVersionId=@id;UPDATE dbo.ContentItems SET Status='Stale' WHERE ChapterId=@ch AND OwnerUserId IS NULL AND Status='Published';INSERT dbo.ContentGaps(CourseId,ChapterId,Kind,Description) SELECT CourseId,@ch,'NewConcept',N'Phiên bản slide mới đã phát hành: cần đối chiếu và dựng lại nội dung liên quan.' FROM dbo.Chapters WHERE ChapterId=@ch;INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'PublishSlides','SlideVersion',CONVERT(nvarchar(100),@id));END;COMMIT;",("@id",id),("@u",Uid));return Redirect("/edu/admin-slides");
 }
 [HttpPost("admin-review"),Authorize(AuthenticationSchemes=Scheme,Roles="ContentReviewer,SuperAdmin")]
 public async Task<IActionResult> AdminReview(long id,string decision) {
  if(decision is not("Pass" or "Revise" or "Reject"))return BadRequest();
  await store.Execute("SET XACT_ABORT ON; BEGIN TRAN; IF EXISTS(SELECT 1 FROM dbo.ContentItems WHERE ContentId=@id AND OwnerUserId IS NULL) BEGIN INSERT dbo.ContentReviews(ContentId,ReviewerUserId,ReviewerKind,Decision) VALUES(@id,@u,'Human',@d);UPDATE dbo.ContentItems SET Status=CASE @d WHEN 'Pass' THEN 'Published' WHEN 'Revise' THEN 'Draft' ELSE 'Rejected' END,PublishedAt=CASE WHEN @d='Pass' THEN SYSUTCDATETIME() ELSE NULL END WHERE ContentId=@id; INSERT dbo.AdminAudit(ActorUserId,Action,EntityType,EntityKey) VALUES(@u,'ContentReview','ContentItem',CONVERT(nvarchar(100),@id));END;COMMIT;",("@id",id),("@u",Uid),("@d",decision));return Redirect("/edu/admin-review");
 }
}
