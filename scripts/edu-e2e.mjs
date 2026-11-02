import assert from 'node:assert/strict';
import {randomBytes,createHmac} from 'node:crypto';
import {execFileSync} from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
const base=process.env.EDUPLAY_TEST_URL||'http://localhost:5211';
const suffix=Date.now().toString(36)+randomBytes(3).toString('hex');
const email=`qa-edu-${suffix}@example.test`,email2=`qa-edu-other-${suffix}@example.test`,adminEmail=`qa-edu-admin-${suffix}@example.test`;
const password=randomBytes(24).toString('base64url');
const quote=s=>"N'"+String(s).replaceAll("'","''")+"'";
const sql=q=>execFileSync('sqlcmd',['-S','localhost','-E','-C','-I','-d','EduPlayV2','-b','-h','-1','-W','-Q','SET NOCOUNT ON; '+q],{encoding:'utf8'}).trim();
let passed=0;const check=s=>{passed++;console.log('PASS '+s);};
class Client{
 cookies=new Map();
 async request(route,method='GET',data){
  const headers={Cookie:[...this.cookies].map(([k,v])=>k+'='+v).join('; ')};let body;
  if(data instanceof FormData)body=data;
  else if(data){body=new URLSearchParams(data);headers['Content-Type']='application/x-www-form-urlencoded';}
  const response=await fetch(base+route,{method,headers,body,redirect:'manual'});
  for(const cookie of response.headers.getSetCookie()){const first=cookie.split(';')[0],i=first.indexOf('=');this.cookies.set(first.slice(0,i),first.slice(i+1));}
  return {status:response.status,headers:response.headers,body:await response.text()};
 }
 async page(route){const r=await this.request(route);assert.equal(r.status,200,route+' '+r.body.slice(0,100));return r.body;}
 token(html){const t=html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);assert.ok(t,'CSRF token');return t[1];}
 async post(route,data,from='/edu'){const h=await this.page(from);return this.request(route,'POST',{...data,__RequestVerificationToken:this.token(h)});}
 async register(e){const r=await this.post('/edu/register',{FullName:'QA Edu '+suffix,Email:e,Password:password,ConfirmPassword:password,Semester:'4',Marketing:'true',AcceptTerms:'true',Role:'SuperAdmin',Coin:'9999'},'/edu/register');assert.equal(r.status,302,r.body.slice(0,500));}
}
function makePdf(pages){
 const objs=['','', '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>'],kids=[];
 for(let i=0;i<pages;i++){const stream=`BT /F1 18 Tf 50 750 Td (EduPlay test private page ${i+1}) Tj ET`,c=objs.push(`<< /Length ${Buffer.byteLength(stream)} >>\nstream\n${stream}\nendstream`),p=objs.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents ${c} 0 R >>`);kids.push(p+' 0 R');}
 objs[0]='<< /Type /Catalog /Pages 2 0 R >>';objs[1]=`<< /Type /Pages /Kids [${kids.join(' ')}] /Count ${pages} >>`;
 let pdf='%PDF-1.4\n',offsets=[0];objs.forEach((o,i)=>{offsets.push(Buffer.byteLength(pdf));pdf+=`${i+1} 0 obj\n${o}\nendobj\n`;});const x=Buffer.byteLength(pdf);pdf+=`xref\n0 ${objs.length+1}\n0000000000 65535 f \n`;for(const n of offsets.slice(1))pdf+=String(n).padStart(10,'0')+' 00000 n \n';pdf+=`trailer\n<< /Size ${objs.length+1} /Root 1 0 R >>\nstartxref\n${x}\n%%EOF\n`;return Buffer.from(pdf);
}
const learner=new Client(),other=new Client(),admin=new Client(),guest=new Client();
const pages=['today','tools','courses','slides','flashcards','quiz','links','case','case-results','plan','mock','study-pack','export','video','vault','files','progress','wallet','topup','pricing','settings','states','onboarding','search','admin','admin-reports','admin-review','admin-slides','admin-cases','admin-gaps','admin-users','admin-pricing','admin-finance','admin-roles'];
let privatePaths=[];
try{
 if(!process.env.EDUPLAY_CLEANUP_EMAILS){
 for(const p of pages.filter(p=>p!=='pricing'))await guest.page('/edu/preview/'+p);
 const pricingRedirect=await guest.request('/edu/preview/pricing');assert.equal(pricingRedirect.status,302);assert.equal(pricingRedirect.headers.get('location'),'/edu/preview/wallet#coin-packages');
 assert.ok((await guest.page('/edu/preview/wallet')).includes('id="coin-packages"'));
 for(const p of ['login','register'])await guest.page('/edu/'+p);
 assert.equal((await guest.request('/')).status,302);
 check('37 preview/auth screens render');
 assert.equal((await guest.request('/edu')).status,302);check('learner pages require authentication');
 await learner.register(email);await other.register(email2);
 const uid=Number(sql(`SELECT UserId FROM dbo.Users WHERE Email=${quote(email)}`));
 assert.equal(sql(`SELECT CONCAT(w.Balance,':',r.Code,':',(SELECT COUNT(*) FROM dbo.CoinLedger WHERE UserId=u.UserId)) FROM dbo.Users u JOIN dbo.Wallets w ON w.UserId=u.UserId JOIN dbo.UserRoles ur ON ur.UserId=u.UserId JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE u.UserId=${uid}`),'20:Learner:1');check('registration: hash, fixed learner role, 20 coins, one ledger');
 assert.equal((await learner.request('/edu/settings','POST',{dailyReminder:'true'})).status,400);check('CSRF blocks mutations');
 const course=sql("SELECT CourseId FROM dbo.Courses WHERE Code='MKT101'");
 const on=await learner.post('/edu/onboarding',{courseIds:course,chapter:'4',examDate:'2026-12-01',weekdays:'2'});assert.equal(on.status,302);assert.equal(sql(`SELECT COUNT(*) FROM dbo.Enrollments WHERE UserId=${uid}`),'1');check('onboarding persists course, exam and classroom schedule');
 for(const p of pages.filter(p=>!p.startsWith('admin')&&p!=='pricing'))await learner.page('/edu/'+p);
 check('all authenticated learner screens render');
 assert.equal((await learner.request('/edu/admin')).status,302);check('learner cannot open admin');
 const card=sql('SELECT TOP 1 ContentId FROM dbo.Flashcards ORDER BY ContentId');
 assert.equal((await learner.post('/edu/api/review',{contentId:card,remembered:'true'})).status,200);
 assert.equal(sql(`SELECT IntervalDays FROM dbo.ReviewSchedules WHERE UserId=${uid} AND FlashcardId=${card}`),'1');check('flashcard review schedules next due date');
 const qid=sql('SELECT TOP 1 ContentId FROM dbo.Questions ORDER BY ContentId');
 const q=JSON.parse((await learner.request('/edu/api/question?id='+qid)).body);assert.ok(q.options.length===4);assert.ok(!('IsCorrect' in q.options[0]));
 const wrong=sql(`SELECT TOP 1 OptionId FROM dbo.QuestionOptions WHERE QuestionId=${qid} AND IsCorrect=0`);
 const right=sql(`SELECT TOP 1 OptionId FROM dbo.QuestionOptions WHERE QuestionId=${qid} AND IsCorrect=1`);
 const answered=await learner.post('/edu/api/answer',{questionId:qid,optionId:wrong});assert.equal(answered.status,200,answered.body);assert.equal(JSON.parse(answered.body).correct,false);assert.ok(JSON.parse(answered.body).correctAnswer);assert.ok(JSON.parse(answered.body).explanation);
 assert.equal(sql(`SELECT COUNT(*) FROM dbo.WrongQuestions WHERE UserId=${uid} AND ResolvedAt IS NULL`),'1');
 assert.equal((await learner.post('/edu/api/answer',{questionId:qid,optionId:right})).status,200);assert.equal(sql(`SELECT COUNT(*) FROM dbo.WrongQuestions WHERE UserId=${uid} AND ResolvedAt IS NULL`),'0');check('server grades answers, tracks wrong bank and resolves corrected questions');
 const caseId=sql('SELECT TOP 1 ContentId FROM dbo.Cases ORDER BY ContentId');
 const opts=sql(`SELECT o.CaseOptionId FROM dbo.CaseSteps s JOIN dbo.CaseOptions o ON o.CaseStepId=s.CaseStepId WHERE s.CaseId=${caseId} AND o.Position=1 ORDER BY s.StepNumber`).split(/\s+/).map(Number);
 let runId=0;for(const optionId of opts){const r=await learner.post('/edu/api/case-choice',{optionId,runId});assert.equal(r.status,200,r.body);runId=JSON.parse(r.body).runId;}
 assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'21');
 let replay=0;for(const optionId of opts){const r=await learner.post('/edu/api/case-choice',{optionId,runId:replay});assert.equal(r.status,200,r.body);replay=JSON.parse(r.body).runId;}
 assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'21');check('four-step case completes; replay does not duplicate first-case reward');
 assert.equal((await learner.post('/edu/save-case',{runId})).status,302);assert.equal(sql(`SELECT COUNT(*) FROM dbo.VaultSlots WHERE UserId=${uid} AND CaseRunId IS NOT NULL`),'1');
 await learner.post('/edu/buy-slot',{});assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'11');check('save case and atomic purchase of extra slot');
 const settings=await learner.post('/edu/settings',{dailyReminder:'true',memoryEnabled:'true',reminderTime:'19:30'});assert.equal(settings.status,302);assert.equal(sql(`SELECT CONVERT(varchar(5),ReminderTime) FROM dbo.UserSettings WHERE UserId=${uid}`),'19:30');check('settings persist');
 const vip=await learner.post('/edu/waitlist',{feature:'Video90'});assert.equal(vip.status,302);await learner.post('/edu/waitlist',{feature:'Video90'});assert.equal(sql(`SELECT COUNT(*) FROM dbo.FeatureWaitlist WHERE UserId=${uid}`),'1');check('VIP waitlist is idempotent');
 const slide=sql('SELECT TOP 1 SlideId FROM dbo.Slides ORDER BY SlideId');await learner.post('/edu/note',{slideId:slide,note:'QA private note '+suffix});assert.equal(sql(`SELECT COUNT(*) FROM dbo.SlideNotes WHERE UserId=${uid}`),'1');assert.ok(!(await other.page('/edu/slides?id='+slide)).includes('QA private note '+suffix));check('slide notes are isolated per learner');
 const uploadPage=await learner.page('/edu/files'),form=new FormData();form.set('__RequestVerificationToken',learner.token(uploadPage));form.set('file',new Blob([makePdf(16)],{type:'application/pdf'}),'qa-edu.pdf');
 const upload=await learner.request('/edu/upload','POST',form);assert.equal(upload.status,302,upload.body);
 const docId=sql(`SELECT TOP 1 DocumentId FROM dbo.Documents WHERE OwnerUserId=${uid} ORDER BY DocumentId DESC`);assert.ok(docId);privatePaths.push(sql(`SELECT StorageKey FROM dbo.Documents WHERE DocumentId=${docId}`));
 assert.equal((await learner.request('/edu/document/'+docId)).status,200);assert.equal((await other.request('/edu/document/'+docId)).status,404);check('valid PDF is extracted and download is owner-only');
 const generated=await learner.post('/edu/generate-study',{id:docId});assert.equal(generated.status,302);assert.equal(generated.headers.get('location'),'/edu/quiz');
 const privateQuestion=sql(`SELECT TOP 1 ContentId FROM dbo.ContentItems WHERE OwnerUserId=${uid} AND Kind='Question' ORDER BY ContentId DESC`);assert.ok(privateQuestion);assert.equal((await other.request('/edu/api/question?id='+privateQuestion)).status,404);await learner.page('/edu/flashcards');check('private PDF generates source-grounded demo study material without cross-user access');
 const before=sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`);await learner.post('/edu/unlock-pdf',{id:docId});assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),before);check('insufficient balance leaves PDF charge unchanged');
 await learner.post('/edu/topup',{packageId:'2',provider:'MoMo'});assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),before);check('unconfigured payment gateway never credits coins');
 await learner.post('/edu/report',{contentId:qid,reason:'ExplanationMismatch',details:'QA report '+suffix});assert.equal(sql(`SELECT COUNT(*) FROM dbo.ContentReports WHERE ReporterUserId=${uid}`),'1');check('content report enters 48-hour queue');
 const output=execFileSync('dotnet',[path.resolve(process.env.EDUPLAY_TEST_DLL||'bin/Release/net10.0/PlayEdu.dll'),'--create-edu-admin'],{encoding:'utf8',env:{...process.env,EDUPLAY_ADMIN_EMAIL:adminEmail,EDUPLAY_ADMIN_PASSWORD:password}});
 assert.match(output,/Admin created/);
 const oldAdmin=await admin.request('/edu/admin-login');assert.equal(oldAdmin.status,302);assert.equal(oldAdmin.headers.get('location'),'/edu/login');
 const adminPage=await admin.page('/edu/login');assert.ok(!adminPage.includes('name="totp"'));
 const normalLogin=await guest.post('/edu/login',{Email:email,Password:password},'/edu/login');assert.equal(normalLogin.status,302);assert.equal(normalLogin.headers.get('location'),'/edu');assert.equal((await guest.request('/edu/admin')).status,302);
 const bad=await admin.post('/edu/login',{Email:adminEmail,Password:'incorrect-password'},'/edu/login');assert.equal(bad.status,200);
 const adminLogin=await admin.post('/edu/login',{Email:adminEmail,Password:password},'/edu/login');assert.equal(adminLogin.status,302,adminLogin.body);assert.equal(adminLogin.headers.get('location'),'/edu/admin');for(const p of pages.filter(x=>x.startsWith('admin')))await admin.page('/edu/'+p);check('admin email/password login without OTP; learner and wrong password rejected');
 const lockedClient=new Client();for(let n=0;n<5;n++){assert.equal((await lockedClient.post('/edu/login',{Email:adminEmail,Password:'incorrect-password'},'/edu/login')).status,200);}
 assert.equal((await lockedClient.post('/edu/login',{Email:adminEmail,Password:password},'/edu/login')).status,200);assert.ok(sql(`SELECT LockedUntil FROM dbo.AdminSecurity WHERE UserId=(SELECT UserId FROM dbo.Users WHERE Email=${quote(adminEmail)})`));
 sql(`UPDATE dbo.AdminSecurity SET LockedUntil=NULL,FailedAttempts=0 WHERE UserId=(SELECT UserId FROM dbo.Users WHERE Email=${quote(adminEmail)})`);check('five incorrect admin passwords lock login for 15 minutes');
 const reportId=sql(`SELECT ReportId FROM dbo.ContentReports WHERE ReporterUserId=${uid}`);const res=await admin.post('/edu/admin-report',{id:reportId,confirmed:'false',resolution:'QA reviewed'},'/edu/admin-reports');assert.equal(res.status,302);assert.equal(sql(`SELECT Status FROM dbo.ContentReports WHERE ReportId=${reportId}`),'Dismissed');check('reviewer resolves report with audit trail');
 await learner.post('/edu/report',{contentId:privateQuestion,reason:'Unclear',details:'QA private content report'});
 const validReport=sql(`SELECT MAX(ReportId) FROM dbo.ContentReports WHERE ReporterUserId=${uid}`);
 await admin.post('/edu/admin-report',{id:validReport,confirmed:'true',resolution:'QA verified'},'/edu/admin-reports');await admin.post('/edu/admin-report',{id:validReport,confirmed:'true',resolution:'QA duplicate'},'/edu/admin-reports');assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'16');check('confirmed content report rewards exactly once');
 await learner.post('/edu/unlock-pdf',{id:docId});await learner.post('/edu/unlock-pdf',{id:docId});assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'1');assert.equal(sql(`SELECT COUNT(*) FROM dbo.Slides WHERE DocumentId=${docId}`),'16');check('PDF unlock extracts paid pages and charges only once');
 await admin.post('/edu/admin-user',{id:uid,operation:'coins',amount:'10',reason:'QA adjustment'},'/edu/admin-users');assert.equal(sql(`SELECT Balance FROM dbo.Wallets WHERE UserId=${uid}`),'11');
 const otherId=sql(`SELECT UserId FROM dbo.Users WHERE Email=${quote(email2)}`);await admin.post('/edu/admin-user',{id:otherId,operation:'lock'},'/edu/admin-users');assert.equal((await other.request('/edu')).status,302);await admin.post('/edu/admin-user',{id:otherId,operation:'unlock'},'/edu/admin-users');check('SuperAdmin adjusts ledger and locks learner access');
 const linkId=sql("SELECT TOP 1 ContentId FROM dbo.ContentItems WHERE Kind='ConceptLink' ORDER BY ContentId");assert.ok(linkId);assert.ok((await learner.page('/edu/links')).includes('OVERTIME'));check('concept-link game uses its own content bank');
 assert.equal(sql('SELECT COUNT(*) FROM dbo.vWalletReconciliation WHERE Difference<>0'),'0');check('all wallet balances reconcile to ledger');
 console.log(`${passed} checks passed.`);
 }
}finally{
 const emailList=(process.env.EDUPLAY_CLEANUP_EMAILS?.split(',')??[email,email2,adminEmail]).map(quote).join(',');
 privatePaths=sql(`SELECT d.StorageKey FROM dbo.Documents d JOIN dbo.Users u ON u.UserId=d.OwnerUserId WHERE u.Email IN(${emailList})`).split(/\r?\n/).filter(Boolean);
 sql(`SET XACT_ABORT ON;BEGIN TRAN;DECLARE @u TABLE(Id bigint);INSERT @u SELECT UserId FROM dbo.Users WHERE Email IN(${emailList});
 DELETE FROM dbo.AdminAudit WHERE ActorUserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.ContentReports WHERE ReporterUserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.FeatureWaitlist WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.SlideNotes WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.VaultSlots WHERE UserId IN(SELECT Id FROM @u);
 DELETE cc FROM dbo.CaseChoices cc JOIN dbo.CaseRuns r ON r.CaseRunId=cc.CaseRunId JOIN dbo.StudySessions s ON s.SessionId=r.SessionId WHERE s.UserId IN(SELECT Id FROM @u);
 DELETE r FROM dbo.CaseRuns r JOIN dbo.StudySessions s ON s.SessionId=r.SessionId WHERE s.UserId IN(SELECT Id FROM @u);
 DELETE a FROM dbo.QuestionAnswers a JOIN dbo.SessionItems si ON si.SessionItemId=a.SessionItemId JOIN dbo.StudySessions s ON s.SessionId=si.SessionId WHERE s.UserId IN(SELECT Id FROM @u);
 DELETE a FROM dbo.FlashcardReviews a JOIN dbo.SessionItems si ON si.SessionItemId=a.SessionItemId JOIN dbo.StudySessions s ON s.SessionId=si.SessionId WHERE s.UserId IN(SELECT Id FROM @u);
 DELETE si FROM dbo.SessionItems si JOIN dbo.StudySessions s ON s.SessionId=si.SessionId WHERE s.UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.StudySessions WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.ReviewSchedules WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.WrongQuestions WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.UserConceptProgress WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.DailyActivity WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.CaseCollection WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.QuestionOptions WHERE QuestionId IN(SELECT ContentId FROM dbo.ContentItems WHERE OwnerUserId IN(SELECT Id FROM @u));
 DELETE FROM dbo.Questions WHERE ContentId IN(SELECT ContentId FROM dbo.ContentItems WHERE OwnerUserId IN(SELECT Id FROM @u));
 DELETE FROM dbo.Flashcards WHERE ContentId IN(SELECT ContentId FROM dbo.ContentItems WHERE OwnerUserId IN(SELECT Id FROM @u));
 DELETE FROM dbo.ContentSources WHERE ContentId IN(SELECT ContentId FROM dbo.ContentItems WHERE OwnerUserId IN(SELECT Id FROM @u));
 DELETE FROM dbo.ContentItems WHERE OwnerUserId IN(SELECT Id FROM @u);
 DELETE s FROM dbo.Slides s JOIN dbo.Documents d ON d.DocumentId=s.DocumentId WHERE d.OwnerUserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.Documents WHERE OwnerUserId IN(SELECT Id FROM @u);
 DELETE cm FROM dbo.ClassMeetings cm JOIN dbo.Enrollments e ON e.EnrollmentId=cm.EnrollmentId WHERE e.UserId IN(SELECT Id FROM @u);
 DELETE ex FROM dbo.Exams ex JOIN dbo.Enrollments e ON e.EnrollmentId=ex.EnrollmentId WHERE e.UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.Enrollments WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.RewardClaims WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.AiCalls WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.CoinLedger WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.Wallets WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.UserSettings WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.AdminSecurity WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.UserRoles WHERE UserId IN(SELECT Id FROM @u);
 DELETE FROM dbo.Users WHERE UserId IN(SELECT Id FROM @u);COMMIT;`);
 for(const key of privatePaths){const file=path.resolve('App_Data/edu-private',path.basename(key));if(fs.existsSync(file))fs.unlinkSync(file);}
}
