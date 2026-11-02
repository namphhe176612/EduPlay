-- New database only. Never changes GameDB. Run with sqlcmd -b.
USE master;
GO
IF DB_ID(N'EduPlayV2') IS NULL
    EXEC(N'CREATE DATABASE EduPlayV2');
GO
USE EduPlayV2;
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL
    THROW 51000, 'EduPlayV2 already initialized. Do not rerun the baseline.', 1;
GO
BEGIN TRANSACTION;
CREATE TABLE dbo.SchemaVersions(Version int PRIMARY KEY, AppliedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

-- Accounts, onboarding, staff permissions. No plaintext passwords or seeded accounts.
CREATE TABLE dbo.Universities(UniversityId int IDENTITY PRIMARY KEY, Name nvarchar(200) NOT NULL UNIQUE);
CREATE TABLE dbo.Majors(MajorId int IDENTITY PRIMARY KEY, Name nvarchar(150) NOT NULL UNIQUE);
CREATE TABLE dbo.Users(
 UserId bigint IDENTITY PRIMARY KEY, FullName nvarchar(100) NOT NULL,
 Email nvarchar(255) NOT NULL, NormalizedEmail AS UPPER(LTRIM(RTRIM(Email))) PERSISTED,
 PasswordHash nvarchar(512) NULL, EmailVerifiedAt datetime2(3) NULL,
 UniversityId int NULL REFERENCES dbo.Universities, MajorId int NULL REFERENCES dbo.Majors,
 CurrentSemester smallint NULL CHECK(CurrentSemester>0), AcquisitionSource nvarchar(100) NULL,
 ReferralCode varchar(32) NOT NULL UNIQUE, ReferredByUserId bigint NULL REFERENCES dbo.Users,
 MarketingConsent bit NOT NULL DEFAULT 0, TermsVersion varchar(30) NOT NULL,
 TermsAcceptedAt datetime2(3) NOT NULL, OnboardingStep tinyint NOT NULL DEFAULT 1 CHECK(OnboardingStep BETWEEN 1 AND 3),
 Status varchar(20) NOT NULL DEFAULT 'Active' CHECK(Status IN('Active','Locked','DeletionPending','Deleted')),
 DeletionRequestedAt datetime2(3) NULL, DeleteAfter datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), Version rowversion,
 CONSTRAINT UQ_Users_Email UNIQUE(NormalizedEmail));
CREATE TABLE dbo.ExternalLogins(UserId bigint NOT NULL REFERENCES dbo.Users, Provider varchar(30) NOT NULL,
 ProviderSubject nvarchar(255) NOT NULL, PRIMARY KEY(Provider,ProviderSubject), UNIQUE(UserId,Provider));
CREATE TABLE dbo.AuthTokens(TokenId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 Purpose varchar(30) NOT NULL CHECK(Purpose IN('ResetPassword','VerifyEmail','Session','TrustedDevice')),
 TokenHash binary(32) NOT NULL UNIQUE, ExpiresAt datetime2(3) NOT NULL, RevokedAt datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.Roles(RoleId int IDENTITY PRIMARY KEY, Code varchar(40) NOT NULL UNIQUE);
CREATE TABLE dbo.Permissions(PermissionId int IDENTITY PRIMARY KEY, Code varchar(80) NOT NULL UNIQUE);
CREATE TABLE dbo.RolePermissions(RoleId int NOT NULL REFERENCES dbo.Roles, PermissionId int NOT NULL REFERENCES dbo.Permissions, PRIMARY KEY(RoleId,PermissionId));
CREATE TABLE dbo.UserRoles(UserId bigint NOT NULL REFERENCES dbo.Users, RoleId int NOT NULL REFERENCES dbo.Roles, GrantedBy bigint NULL REFERENCES dbo.Users,
 GrantedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,RoleId));
CREATE TABLE dbo.AdminSecurity(UserId bigint PRIMARY KEY REFERENCES dbo.Users, EncryptedTotpSecret varbinary(1024) NULL,
 MfaEnabledAt datetime2(3) NULL, FailedAttempts int NOT NULL DEFAULT 0 CHECK(FailedAttempts>=0), LockedUntil datetime2(3) NULL);
CREATE TABLE dbo.AdminAudit(AuditId bigint IDENTITY PRIMARY KEY, ActorUserId bigint NULL REFERENCES dbo.Users,
 Action varchar(100) NOT NULL, EntityType varchar(60) NULL, EntityKey nvarchar(100) NULL,
 MetadataJson nvarchar(max) NULL CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1),
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

-- Curriculum and the learner's actual classroom schedule.
CREATE TABLE dbo.Courses(CourseId int IDENTITY PRIMARY KEY, Code varchar(30) NOT NULL UNIQUE, Name nvarchar(200) NOT NULL,
 MajorId int NULL REFERENCES dbo.Majors, IsAvailable bit NOT NULL DEFAULT 1);
CREATE TABLE dbo.Chapters(ChapterId int IDENTITY PRIMARY KEY, CourseId int NOT NULL REFERENCES dbo.Courses,
 ChapterNumber smallint NOT NULL CHECK(ChapterNumber>0), Title nvarchar(200) NOT NULL,
 WeekFrom smallint NULL CHECK(WeekFrom>0), WeekTo smallint NULL, UNIQUE(CourseId,ChapterNumber),
 CHECK(WeekTo IS NULL OR WeekTo>=WeekFrom));
CREATE TABLE dbo.Enrollments(EnrollmentId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 CourseId int NOT NULL REFERENCES dbo.Courses, CurrentChapterId int NULL REFERENCES dbo.Chapters,
 IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(UserId,CourseId));
CREATE TABLE dbo.ClassMeetings(ClassMeetingId bigint IDENTITY PRIMARY KEY, EnrollmentId bigint NOT NULL REFERENCES dbo.Enrollments,
 IsoWeekday tinyint NOT NULL CHECK(IsoWeekday BETWEEN 1 AND 7), StartTime time(0) NULL, EndTime time(0) NULL,
 UNIQUE(EnrollmentId,IsoWeekday), CHECK(EndTime IS NULL OR EndTime>StartTime));
CREATE TABLE dbo.Exams(ExamId bigint IDENTITY PRIMARY KEY, EnrollmentId bigint NOT NULL REFERENCES dbo.Enrollments,
 Kind varchar(15) NOT NULL CHECK(Kind IN('Midterm','Final','Other')), Name nvarchar(150) NOT NULL,
 ExamDate date NULL, TargetReadiness decimal(5,2) NOT NULL DEFAULT 85 CHECK(TargetReadiness BETWEEN 0 AND 100));
CREATE TABLE dbo.ExamChapters(ExamId bigint NOT NULL REFERENCES dbo.Exams, ChapterId int NOT NULL REFERENCES dbo.Chapters,
 Weight decimal(5,2) NULL CHECK(Weight BETWEEN 0 AND 100), PRIMARY KEY(ExamId,ChapterId));

-- Public lecturer materials and private uploads are distinct security scopes.
CREATE TABLE dbo.Documents(DocumentId bigint IDENTITY PRIMARY KEY, OwnerUserId bigint NULL REFERENCES dbo.Users,
 ChapterId int NULL REFERENCES dbo.Chapters, Scope varchar(10) NOT NULL CHECK(Scope IN('Library','Private')),
 Kind varchar(15) NOT NULL DEFAULT 'Slides' CHECK(Kind IN('Slides','Notes','ExamOutline')),
 OriginalName nvarchar(255) NOT NULL, StorageKey nvarchar(500) NOT NULL, Sha256 binary(32) NULL,
 ByteLength bigint NOT NULL CHECK(ByteLength>0), PageCount int NULL CHECK(PageCount>0),
 UnlockedPages int NOT NULL DEFAULT 15 CHECK(UnlockedPages>0), DeletedAt datetime2(3) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
 CHECK((Scope='Private' AND OwnerUserId IS NOT NULL) OR (Scope='Library' AND OwnerUserId IS NULL AND ChapterId IS NOT NULL)));
CREATE TABLE dbo.SlideVersions(SlideVersionId bigint IDENTITY PRIMARY KEY, ChapterId int NOT NULL REFERENCES dbo.Chapters,
 DocumentId bigint NOT NULL UNIQUE REFERENCES dbo.Documents, VersionNumber int NOT NULL CHECK(VersionNumber>0),
 Status varchar(20) NOT NULL DEFAULT 'Draft' CHECK(Status IN('Draft','Processing','Review','Published','Superseded','Failed')),
 UploadedBy bigint NOT NULL REFERENCES dbo.Users, PublishedAt datetime2(3) NULL, ChangeSummary nvarchar(1000) NULL,
 UNIQUE(ChapterId,VersionNumber));
CREATE UNIQUE INDEX UX_SlideVersions_Current ON dbo.SlideVersions(ChapterId) WHERE Status='Published';
CREATE TABLE dbo.Slides(SlideId bigint IDENTITY PRIMARY KEY, DocumentId bigint NOT NULL REFERENCES dbo.Documents,
 PageNumber int NOT NULL CHECK(PageNumber>0), ExtractedText nvarchar(max) NULL, ImageStorageKey nvarchar(500) NULL,
 TextHash binary(32) NULL, UNIQUE(DocumentId,PageNumber));
CREATE TABLE dbo.SlideNotes(UserId bigint NOT NULL REFERENCES dbo.Users, SlideId bigint NOT NULL REFERENCES dbo.Slides,
 Note nvarchar(max) NOT NULL, HighlightJson nvarchar(max) NULL CHECK(HighlightJson IS NULL OR ISJSON(HighlightJson)=1),
 UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,SlideId));
CREATE TABLE dbo.Concepts(ConceptId int IDENTITY PRIMARY KEY, CourseId int NOT NULL REFERENCES dbo.Courses,
 EnglishTerm nvarchar(200) NOT NULL, VietnameseTerm nvarchar(200) NOT NULL, Definition nvarchar(max) NOT NULL,
 UNIQUE(CourseId,EnglishTerm));
CREATE TABLE dbo.ConceptSlides(ConceptId int NOT NULL REFERENCES dbo.Concepts, SlideId bigint NOT NULL REFERENCES dbo.Slides,
 SourceQuote nvarchar(2000) NOT NULL, PRIMARY KEY(ConceptId,SlideId));

-- Every generated item has a version, publication gate and explicit slide citations.
CREATE TABLE dbo.ContentItems(ContentId bigint IDENTITY PRIMARY KEY, ChapterId int NULL REFERENCES dbo.Chapters,
 OwnerUserId bigint NULL REFERENCES dbo.Users, SourceDocumentId bigint NULL REFERENCES dbo.Documents,
 Kind varchar(20) NOT NULL CHECK(Kind IN('Flashcard','Question','Case','Video','ConceptLink','StudyPack')),
 Title nvarchar(250) NOT NULL, Revision int NOT NULL DEFAULT 1 CHECK(Revision>0),
 SupersedesContentId bigint NULL REFERENCES dbo.ContentItems,
 Status varchar(20) NOT NULL DEFAULT 'Draft' CHECK(Status IN('Draft','Generating','Review','Published','Rejected','Hidden','Stale','Failed')),
 CreatedBy bigint NULL REFERENCES dbo.Users, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PublishedAt datetime2(3) NULL,
 CHECK(ChapterId IS NOT NULL OR (OwnerUserId IS NOT NULL AND SourceDocumentId IS NOT NULL)));
CREATE TABLE dbo.ContentSources(ContentId bigint NOT NULL REFERENCES dbo.ContentItems, SlideId bigint NOT NULL REFERENCES dbo.Slides,
 SourceQuote nvarchar(2000) NULL, PRIMARY KEY(ContentId,SlideId));
CREATE TABLE dbo.ContentConcepts(ContentId bigint NOT NULL REFERENCES dbo.ContentItems, ConceptId int NOT NULL REFERENCES dbo.Concepts,
 PRIMARY KEY(ContentId,ConceptId));
CREATE TABLE dbo.Flashcards(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems, Front nvarchar(2000) NOT NULL, Back nvarchar(max) NOT NULL);
CREATE TABLE dbo.Questions(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems,
 Format varchar(20) NOT NULL CHECK(Format IN('MultipleChoice','ShortAnswer','Situation')),
 Prompt nvarchar(max) NOT NULL, Explanation nvarchar(max) NOT NULL, RubricJson nvarchar(max) NULL CHECK(RubricJson IS NULL OR ISJSON(RubricJson)=1));
CREATE TABLE dbo.QuestionOptions(OptionId bigint IDENTITY PRIMARY KEY, QuestionId bigint NOT NULL REFERENCES dbo.Questions,
 Position tinyint NOT NULL CHECK(Position>0), Text nvarchar(2000) NOT NULL, IsCorrect bit NOT NULL DEFAULT 0, UNIQUE(QuestionId,Position), UNIQUE(QuestionId,OptionId));
CREATE TABLE dbo.ArticleSources(ArticleId bigint IDENTITY PRIMARY KEY, Url nvarchar(1500) NOT NULL,
 UrlHash binary(32) NOT NULL UNIQUE, Title nvarchar(300) NOT NULL, Publisher nvarchar(200) NOT NULL,
 PublishedDate date NULL, Reliability decimal(3,2) NOT NULL CHECK(Reliability BETWEEN 0 AND 1),
 IsAllowed bit NOT NULL DEFAULT 0, RetrievedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.Cases(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems, ArticleId bigint NULL REFERENCES dbo.ArticleSources,
 CompanyName nvarchar(200) NOT NULL, EventYear smallint NULL CHECK(EventYear BETWEEN 1900 AND 2200),
 IsHypothetical bit NOT NULL, Premise nvarchar(max) NOT NULL, QualityScore decimal(4,3) NULL CHECK(QualityScore BETWEEN 0 AND 1),
 ScoreBreakdownJson nvarchar(max) NULL CHECK(ScoreBreakdownJson IS NULL OR ISJSON(ScoreBreakdownJson)=1),
 CHECK(IsHypothetical=1 OR ArticleId IS NOT NULL));
CREATE TABLE dbo.CaseSteps(CaseStepId bigint IDENTITY PRIMARY KEY, CaseId bigint NOT NULL REFERENCES dbo.Cases,
 StepNumber tinyint NOT NULL CHECK(StepNumber BETWEEN 1 AND 4), Skill varchar(20) NOT NULL CHECK(Skill IN('Identify','Explain','Connect','Apply')),
 Speaker nvarchar(100) NOT NULL, Prompt nvarchar(max) NOT NULL, UNIQUE(CaseId,StepNumber), UNIQUE(CaseId,CaseStepId));
CREATE TABLE dbo.CaseOptions(CaseOptionId bigint IDENTITY PRIMARY KEY, CaseStepId bigint NOT NULL REFERENCES dbo.CaseSteps,
 Position tinyint NOT NULL CHECK(Position>0), Text nvarchar(2000) NOT NULL, Feedback nvarchar(max) NOT NULL,
 Quality decimal(4,3) NOT NULL CHECK(Quality BETWEEN 0 AND 1), MetricDeltasJson nvarchar(max) NULL CHECK(MetricDeltasJson IS NULL OR ISJSON(MetricDeltasJson)=1),
 UNIQUE(CaseStepId,Position), UNIQUE(CaseStepId,CaseOptionId));
CREATE TABLE dbo.ConceptLinks(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems, FromConceptId int NOT NULL REFERENCES dbo.Concepts,
 ToConceptId int NOT NULL REFERENCES dbo.Concepts, Explanation nvarchar(2000) NOT NULL, CHECK(FromConceptId<>ToConceptId));
CREATE TABLE dbo.Videos(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems, ConceptId int NULL REFERENCES dbo.Concepts,
 Style varchar(30) NOT NULL, Voice varchar(30) NOT NULL, Language varchar(10) NOT NULL,
 DurationSeconds int NOT NULL DEFAULT 90 CHECK(DurationSeconds>0), VideoStorageKey nvarchar(500) NULL,
 ScriptJson nvarchar(max) NULL CHECK(ScriptJson IS NULL OR ISJSON(ScriptJson)=1));

-- Learning history, spaced repetition, wrong-answer bank and case collection.
CREATE TABLE dbo.StudySessions(SessionId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 EnrollmentId bigint NULL REFERENCES dbo.Enrollments, Kind varchar(25) NOT NULL CHECK(Kind IN('Flashcard','Quiz','ConceptLink','DeepPractice','MockExam','Case')),
 Status varchar(15) NOT NULL DEFAULT 'Active' CHECK(Status IN('Active','Completed','Abandoned')),
 StartedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CompletedAt datetime2(3) NULL,
 TimeLimitSeconds int NULL CHECK(TimeLimitSeconds>0), Score decimal(7,2) NULL, PossibleScore decimal(7,2) NULL,
 CHECK(CompletedAt IS NULL OR CompletedAt>=StartedAt));
CREATE TABLE dbo.SessionItems(SessionItemId bigint IDENTITY PRIMARY KEY, SessionId bigint NOT NULL REFERENCES dbo.StudySessions,
 ContentId bigint NOT NULL REFERENCES dbo.ContentItems, Position int NOT NULL CHECK(Position>0),
 Flagged bit NOT NULL DEFAULT 0, UNIQUE(SessionId,Position), UNIQUE(SessionId,SessionItemId));
CREATE TABLE dbo.QuestionAnswers(AnswerId bigint IDENTITY PRIMARY KEY, SessionItemId bigint NOT NULL UNIQUE REFERENCES dbo.SessionItems,
 QuestionId bigint NOT NULL REFERENCES dbo.Questions, SelectedOptionId bigint NULL, AnswerText nvarchar(max) NULL,
 IsCorrect bit NULL, ExcludedFromReadiness bit NOT NULL DEFAULT 0, AnsweredAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
 FOREIGN KEY(QuestionId,SelectedOptionId) REFERENCES dbo.QuestionOptions(QuestionId,OptionId));
CREATE TABLE dbo.FlashcardReviews(ReviewId bigint IDENTITY PRIMARY KEY, SessionItemId bigint NOT NULL UNIQUE REFERENCES dbo.SessionItems,
 Remembered bit NOT NULL, ReviewedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), NextDueAt datetime2(3) NOT NULL);
CREATE TABLE dbo.ReviewSchedules(UserId bigint NOT NULL REFERENCES dbo.Users, FlashcardId bigint NOT NULL REFERENCES dbo.Flashcards,
 IntervalDays int NOT NULL DEFAULT 0 CHECK(IntervalDays>=0), Repetitions int NOT NULL DEFAULT 0 CHECK(Repetitions>=0),
 NextDueAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), LastReviewedAt datetime2(3) NULL, PRIMARY KEY(UserId,FlashcardId));
CREATE TABLE dbo.UserConceptProgress(UserId bigint NOT NULL REFERENCES dbo.Users, ConceptId int NOT NULL REFERENCES dbo.Concepts,
 CorrectCount int NOT NULL DEFAULT 0 CHECK(CorrectCount>=0), WrongCount int NOT NULL DEFAULT 0 CHECK(WrongCount>=0),
 Mastery decimal(5,2) NOT NULL DEFAULT 0 CHECK(Mastery BETWEEN 0 AND 100), UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,ConceptId));
CREATE TABLE dbo.WrongQuestions(UserId bigint NOT NULL REFERENCES dbo.Users, QuestionId bigint NOT NULL REFERENCES dbo.Questions,
 WrongCount int NOT NULL DEFAULT 1 CHECK(WrongCount>0), LastWrongAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
 ResolvedAt datetime2(3) NULL, PRIMARY KEY(UserId,QuestionId));
CREATE TABLE dbo.CaseRuns(CaseRunId bigint IDENTITY PRIMARY KEY, SessionId bigint NOT NULL UNIQUE REFERENCES dbo.StudySessions,
 CaseId bigint NOT NULL REFERENCES dbo.Cases, StepsPassed tinyint NOT NULL DEFAULT 0 CHECK(StepsPassed BETWEEN 0 AND 4),
 SlideHintsUsed tinyint NOT NULL DEFAULT 0 CHECK(SlideHintsUsed BETWEEN 0 AND 2), UNIQUE(CaseRunId,CaseId));
CREATE TABLE dbo.CaseChoices(CaseRunId bigint NOT NULL, CaseId bigint NOT NULL, CaseStepId bigint NOT NULL,
 CaseOptionId bigint NOT NULL, ChosenAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(CaseRunId,CaseStepId),
 FOREIGN KEY(CaseRunId,CaseId) REFERENCES dbo.CaseRuns(CaseRunId,CaseId),
 FOREIGN KEY(CaseId,CaseStepId) REFERENCES dbo.CaseSteps(CaseId,CaseStepId),
 FOREIGN KEY(CaseStepId,CaseOptionId) REFERENCES dbo.CaseOptions(CaseStepId,CaseOptionId));
CREATE TABLE dbo.CaseCollection(UserId bigint NOT NULL REFERENCES dbo.Users, CaseId bigint NOT NULL REFERENCES dbo.Cases,
 FirstCompletedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,CaseId));
CREATE TABLE dbo.DailyActivity(UserId bigint NOT NULL REFERENCES dbo.Users, LocalDate date NOT NULL,
 StudySeconds int NOT NULL DEFAULT 0 CHECK(StudySeconds>=0), CompletedSessions int NOT NULL DEFAULT 0 CHECK(CompletedSessions>=0), PRIMARY KEY(UserId,LocalDate));
CREATE TABLE dbo.VaultSlots(UserId bigint NOT NULL REFERENCES dbo.Users, SlotNumber tinyint NOT NULL CHECK(SlotNumber BETWEEN 1 AND 9),
 CaseRunId bigint NULL REFERENCES dbo.CaseRuns, AcquiredAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,SlotNumber));
CREATE UNIQUE INDEX UX_VaultSlots_Run ON dbo.VaultSlots(UserId,CaseRunId) WHERE CaseRunId IS NOT NULL;
CREATE TABLE dbo.SavedItems(SavedItemId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 ContentId bigint NOT NULL REFERENCES dbo.ContentItems, SavedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(UserId,ContentId));

-- Pricing, immutable ledger, idempotent payment events and metered free usage.
CREATE TABLE dbo.Wallets(UserId bigint PRIMARY KEY REFERENCES dbo.Users, Balance int NOT NULL DEFAULT 0 CHECK(Balance>=0), Version rowversion);
CREATE TABLE dbo.CoinLedger(LedgerId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Wallets,
 Amount int NOT NULL CHECK(Amount<>0), BalanceAfter int NOT NULL CHECK(BalanceAfter>=0),
 Reason varchar(40) NOT NULL, IdempotencyKey varchar(150) NOT NULL UNIQUE, ReferenceType varchar(40) NULL,
 ReferenceId nvarchar(100) NULL, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.CoinPackages(PackageId int IDENTITY PRIMARY KEY, Code varchar(30) NOT NULL UNIQUE, Name nvarchar(100) NOT NULL,
 Coins int NOT NULL CHECK(Coins>0), PriceVnd int NOT NULL CHECK(PriceVnd>0), IsActive bit NOT NULL DEFAULT 1);
CREATE TABLE dbo.Promotions(PromotionId int IDENTITY PRIMARY KEY, Code varchar(40) NOT NULL UNIQUE,
 DiscountVnd int NOT NULL DEFAULT 0 CHECK(DiscountVnd>=0), BonusCoins int NOT NULL DEFAULT 0 CHECK(BonusCoins>=0),
 StartsAt datetime2(3) NOT NULL, EndsAt datetime2(3) NOT NULL, MaxRedemptions int NULL CHECK(MaxRedemptions>0), CHECK(EndsAt>StartsAt));
CREATE TABLE dbo.Payments(PaymentId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 PackageId int NOT NULL REFERENCES dbo.CoinPackages, PromotionId int NULL REFERENCES dbo.Promotions,
 MerchantReference varchar(100) NOT NULL UNIQUE, Provider varchar(30) NOT NULL,
 ProviderTransactionId varchar(150) NULL, PriceVnd int NOT NULL CHECK(PriceVnd>0), Coins int NOT NULL CHECK(Coins>0),
 Status varchar(20) NOT NULL DEFAULT 'Pending' CHECK(Status IN('Pending','Paid','Failed','Cancelled','Refunded')),
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PaidAt datetime2(3) NULL,
 LedgerId bigint NULL REFERENCES dbo.CoinLedger);
CREATE UNIQUE INDEX UX_Payments_Ledger ON dbo.Payments(LedgerId) WHERE LedgerId IS NOT NULL;
CREATE UNIQUE INDEX UX_Payments_ProviderTxn ON dbo.Payments(Provider,ProviderTransactionId) WHERE ProviderTransactionId IS NOT NULL;
CREATE TABLE dbo.PaymentEvents(PaymentEventId bigint IDENTITY PRIMARY KEY, PaymentId bigint NOT NULL REFERENCES dbo.Payments,
 Provider varchar(30) NOT NULL, ProviderEventId varchar(150) NOT NULL, SignatureVerified bit NOT NULL,
 PayloadHash binary(32) NOT NULL, ReceivedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), ProcessedAt datetime2(3) NULL,
 UNIQUE(Provider,ProviderEventId));
CREATE TABLE dbo.FeaturePolicies(FeatureCode varchar(40) PRIMARY KEY, Name nvarchar(150) NOT NULL,
 Availability varchar(15) NOT NULL CHECK(Availability IN('Available','ComingSoon','Disabled')),
 FreeUnits int NOT NULL DEFAULT 0 CHECK(FreeUnits>=0), ResetPeriod varchar(15) NOT NULL CHECK(ResetPeriod IN('None','Daily','PerCase','Lifetime')),
 CoinsPerBlock int NOT NULL DEFAULT 0 CHECK(CoinsPerBlock>=0), UnitsPerBlock int NOT NULL DEFAULT 1 CHECK(UnitsPerBlock>0));
CREATE TABLE dbo.UsageCounters(UserId bigint NOT NULL REFERENCES dbo.Users, FeatureCode varchar(40) NOT NULL REFERENCES dbo.FeaturePolicies,
 ScopeKey varchar(100) NOT NULL, PeriodStart date NOT NULL, UsedUnits int NOT NULL DEFAULT 0 CHECK(UsedUnits>=0),
 PRIMARY KEY(UserId,FeatureCode,ScopeKey,PeriodStart));
CREATE TABLE dbo.RewardClaims(RewardClaimId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 RewardCode varchar(40) NOT NULL, ScopeKey varchar(100) NOT NULL, LedgerId bigint NOT NULL UNIQUE REFERENCES dbo.CoinLedger,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(UserId,RewardCode,ScopeKey));
CREATE TABLE dbo.Referrals(ReferralId bigint IDENTITY PRIMARY KEY, ReferrerUserId bigint NOT NULL REFERENCES dbo.Users,
 ReferredUserId bigint NOT NULL UNIQUE REFERENCES dbo.Users, QualifiedAt datetime2(3) NULL,
 RewardClaimId bigint NULL REFERENCES dbo.RewardClaims, CHECK(ReferrerUserId<>ReferredUserId));
CREATE UNIQUE INDEX UX_Referrals_Reward ON dbo.Referrals(RewardClaimId) WHERE RewardClaimId IS NOT NULL;

-- Async AI work is tracked separately from transactions; no SQL lock during provider calls.
CREATE TABLE dbo.Jobs(JobId bigint IDENTITY PRIMARY KEY, RequestedBy bigint NULL REFERENCES dbo.Users,
 Kind varchar(40) NOT NULL, DocumentId bigint NULL REFERENCES dbo.Documents, ContentId bigint NULL REFERENCES dbo.ContentItems,
 Status varchar(20) NOT NULL DEFAULT 'Queued' CHECK(Status IN('Queued','Running','Succeeded','Failed','Cancelled')),
 Progress tinyint NOT NULL DEFAULT 0 CHECK(Progress BETWEEN 0 AND 100), Attempt int NOT NULL DEFAULT 0 CHECK(Attempt>=0),
 ErrorCode varchar(80) NULL, IdempotencyKey varchar(150) NOT NULL UNIQUE,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), StartedAt datetime2(3) NULL, FinishedAt datetime2(3) NULL);
CREATE TABLE dbo.AiCalls(AiCallId bigint IDENTITY PRIMARY KEY, UserId bigint NULL REFERENCES dbo.Users, JobId bigint NULL REFERENCES dbo.Jobs,
 FeatureCode varchar(40) NOT NULL REFERENCES dbo.FeaturePolicies, Provider varchar(50) NOT NULL, Model nvarchar(100) NOT NULL,
 InputTokens int NOT NULL DEFAULT 0 CHECK(InputTokens>=0), OutputTokens int NOT NULL DEFAULT 0 CHECK(OutputTokens>=0),
 CostUsd decimal(19,8) NOT NULL DEFAULT 0 CHECK(CostUsd>=0), DurationMs int NULL CHECK(DurationMs>=0),
 IsSuccess bit NOT NULL, ErrorCode varchar(80) NULL, LedgerId bigint NULL REFERENCES dbo.CoinLedger,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.AiConversations(ConversationId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 CaseId bigint NULL REFERENCES dbo.Cases, DocumentId bigint NULL REFERENCES dbo.Documents,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.AiMessages(MessageId bigint IDENTITY PRIMARY KEY, ConversationId bigint NOT NULL REFERENCES dbo.AiConversations,
 Role varchar(12) NOT NULL CHECK(Role IN('User','Assistant')), Text nvarchar(max) NOT NULL,
 AiCallId bigint NULL REFERENCES dbo.AiCalls, IsSaved bit NOT NULL DEFAULT 0, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.MessageSources(MessageId bigint NOT NULL REFERENCES dbo.AiMessages, SlideId bigint NOT NULL REFERENCES dbo.Slides,
 SourceQuote nvarchar(2000) NULL, PRIMARY KEY(MessageId,SlideId));

-- Editorial workflow and the 48-hour learner feedback loop.
CREATE TABLE dbo.ContentReviews(ReviewId bigint IDENTITY PRIMARY KEY, ContentId bigint NOT NULL REFERENCES dbo.ContentItems,
 ReviewerUserId bigint NULL REFERENCES dbo.Users, ReviewerKind varchar(10) NOT NULL CHECK(ReviewerKind IN('Human','LLM')),
 Decision varchar(15) NOT NULL CHECK(Decision IN('Pass','Revise','Reject')), Notes nvarchar(2000) NULL,
 ChecklistJson nvarchar(max) NULL CHECK(ChecklistJson IS NULL OR ISJSON(ChecklistJson)=1),
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), CHECK(ReviewerKind='LLM' OR ReviewerUserId IS NOT NULL));
CREATE TABLE dbo.ContentReports(ReportId bigint IDENTITY PRIMARY KEY, ReporterUserId bigint NOT NULL REFERENCES dbo.Users,
 ContentId bigint NULL REFERENCES dbo.ContentItems, SlideId bigint NULL REFERENCES dbo.Slides,
 AnswerId bigint NULL REFERENCES dbo.QuestionAnswers, Reason varchar(30) NOT NULL CHECK(Reason IN('WrongGrading','ExplanationMismatch','MissingSource','Unclear','Other')),
 Details nvarchar(2000) NULL, Status varchar(20) NOT NULL DEFAULT 'Open' CHECK(Status IN('Open','Investigating','Confirmed','Dismissed')),
 AssignedTo bigint NULL REFERENCES dbo.Users, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
 DueAt datetime2(3) NOT NULL DEFAULT DATEADD(hour,48,SYSUTCDATETIME()), ResolvedAt datetime2(3) NULL,
 Resolution nvarchar(2000) NULL, RewardClaimId bigint NULL REFERENCES dbo.RewardClaims,
 CHECK(ContentId IS NOT NULL OR SlideId IS NOT NULL));
CREATE UNIQUE INDEX UX_Reports_Reward ON dbo.ContentReports(RewardClaimId) WHERE RewardClaimId IS NOT NULL;
CREATE TABLE dbo.ContentGaps(GapId bigint IDENTITY PRIMARY KEY, CourseId int NOT NULL REFERENCES dbo.Courses,
 ChapterId int NULL REFERENCES dbo.Chapters, ConceptId int NULL REFERENCES dbo.Concepts,
 Kind varchar(30) NOT NULL CHECK(Kind IN('NewConcept','MissingSource','MissingCase','HypotheticalLabel','SearchMiss')),
 Description nvarchar(2000) NOT NULL, Status varchar(15) NOT NULL DEFAULT 'Open' CHECK(Status IN('Open','Assigned','Resolved','Dismissed')),
 AssignedTo bigint NULL REFERENCES dbo.Users, DueAt datetime2(3) NULL, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.SearchEvents(SearchEventId bigint IDENTITY PRIMARY KEY, UserId bigint NULL REFERENCES dbo.Users,
 CourseId int NULL REFERENCES dbo.Courses, Query nvarchar(300) NOT NULL, ResultCount int NOT NULL CHECK(ResultCount>=0),
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

-- Settings, memory, reminders and VIP preview data.
CREATE TABLE dbo.UserSettings(UserId bigint PRIMARY KEY REFERENCES dbo.Users, TimeZoneId varchar(80) NOT NULL DEFAULT 'Asia/Bangkok',
 DailyReminder bit NOT NULL DEFAULT 1, ReminderTime time(0) NOT NULL DEFAULT '21:00', AfterClassReminder bit NOT NULL DEFAULT 1,
 BeforeExamReminder bit NOT NULL DEFAULT 1, SlideUpdateReminder bit NOT NULL DEFAULT 0,
 EmailNotifications bit NOT NULL DEFAULT 1, BrowserNotifications bit NOT NULL DEFAULT 0, MemoryEnabled bit NOT NULL DEFAULT 1);
CREATE TABLE dbo.UserMemories(MemoryId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 Text nvarchar(1000) NOT NULL, Origin varchar(30) NOT NULL, DeletedAt datetime2(3) NULL, CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.Notifications(NotificationId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 Kind varchar(40) NOT NULL, Channel varchar(10) NOT NULL CHECK(Channel IN('InApp','Email','Browser')),
 Title nvarchar(200) NOT NULL, Body nvarchar(2000) NULL, ScheduledAt datetime2(3) NOT NULL,
 Status varchar(12) NOT NULL DEFAULT 'Pending' CHECK(Status IN('Pending','Sent','Failed','Cancelled')),
 SentAt datetime2(3) NULL, ReadAt datetime2(3) NULL, DedupeKey varchar(150) NOT NULL UNIQUE);
CREATE TABLE dbo.FeatureWaitlist(UserId bigint NOT NULL REFERENCES dbo.Users, FeatureCode varchar(40) NOT NULL REFERENCES dbo.FeaturePolicies,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(UserId,FeatureCode));
CREATE TABLE dbo.StudyPlans(PlanId bigint IDENTITY PRIMARY KEY, ExamId bigint NOT NULL REFERENCES dbo.Exams,
 MinutesPerDay smallint NOT NULL DEFAULT 20 CHECK(MinutesPerDay>0), RestIsoWeekday tinyint NULL CHECK(RestIsoWeekday BETWEEN 1 AND 7),
 Status varchar(15) NOT NULL DEFAULT 'Preview' CHECK(Status IN('Preview','Active','Completed','Cancelled')),
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.StudyTasks(TaskId bigint IDENTITY PRIMARY KEY, PlanId bigint NOT NULL REFERENCES dbo.StudyPlans,
 LocalDate date NOT NULL, Kind varchar(30) NOT NULL, ChapterId int NULL REFERENCES dbo.Chapters,
 ContentId bigint NULL REFERENCES dbo.ContentItems, EstimatedMinutes smallint NOT NULL CHECK(EstimatedMinutes>0),
 CompletedAt datetime2(3) NULL, Position smallint NOT NULL CHECK(Position>0), UNIQUE(PlanId,LocalDate,Position));
CREATE TABLE dbo.StudyPacks(ContentId bigint PRIMARY KEY REFERENCES dbo.ContentItems, ExamId bigint NOT NULL REFERENCES dbo.Exams,
 OutlineDocumentId bigint NULL REFERENCES dbo.Documents, MatchedQuestions int NOT NULL DEFAULT 0 CHECK(MatchedQuestions>=0),
 UnmatchedQuestions int NOT NULL DEFAULT 0 CHECK(UnmatchedQuestions>=0));
CREATE TABLE dbo.StudyPackItems(PackContentId bigint NOT NULL REFERENCES dbo.StudyPacks,
 ContentId bigint NOT NULL REFERENCES dbo.ContentItems, Position int NOT NULL CHECK(Position>0), PRIMARY KEY(PackContentId,ContentId), UNIQUE(PackContentId,Position));
CREATE TABLE dbo.PdfExports(ExportId bigint IDENTITY PRIMARY KEY, UserId bigint NOT NULL REFERENCES dbo.Users,
 PackContentId bigint NOT NULL REFERENCES dbo.StudyPacks, JobId bigint NULL REFERENCES dbo.Jobs,
 OptionsJson nvarchar(max) NOT NULL CHECK(ISJSON(OptionsJson)=1), StorageKey nvarchar(500) NULL,
 CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.MockExams(SessionId bigint PRIMARY KEY REFERENCES dbo.StudySessions, ExamId bigint NOT NULL REFERENCES dbo.Exams,
 SubmittedAt datetime2(3) NULL, GradedAt datetime2(3) NULL);
CREATE TABLE dbo.SystemSettings(SettingKey varchar(100) PRIMARY KEY, Value nvarchar(1000) NOT NULL, Description nvarchar(500) NOT NULL);

CREATE INDEX IX_AuthTokens_Expiry ON dbo.AuthTokens(UserId,Purpose,ExpiresAt);
CREATE INDEX IX_Documents_Owner ON dbo.Documents(OwnerUserId,CreatedAt DESC);
CREATE INDEX IX_Slides_Document ON dbo.Slides(DocumentId) INCLUDE(PageNumber);
CREATE INDEX IX_Content_Chapter ON dbo.ContentItems(ChapterId,Status,Kind);
CREATE INDEX IX_Content_Owner ON dbo.ContentItems(OwnerUserId,Status,Kind);
CREATE INDEX IX_Sessions_History ON dbo.StudySessions(UserId,StartedAt DESC);
CREATE INDEX IX_ReviewSchedules_Due ON dbo.ReviewSchedules(UserId,NextDueAt);
CREATE INDEX IX_Ledger_History ON dbo.CoinLedger(UserId,CreatedAt DESC) INCLUDE(Amount,BalanceAfter,Reason);
CREATE INDEX IX_Reports_Queue ON dbo.ContentReports(Status,DueAt);
CREATE INDEX IX_Jobs_Queue ON dbo.Jobs(Status,CreatedAt);
CREATE INDEX IX_Notifications_Queue ON dbo.Notifications(Status,ScheduledAt);
CREATE INDEX IX_AiCalls_Cost ON dbo.AiCalls(CreatedAt,FeatureCode) INCLUDE(CostUsd,InputTokens,OutputTokens);
INSERT dbo.SchemaVersions(Version) VALUES(1);
COMMIT;
GO
