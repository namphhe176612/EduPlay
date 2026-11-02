USE EduPlayV2;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Only the trusted backend should receive EXECUTE permission. No browser SQL access.
-- Wallet row serializes requests. Ledger and balance always commit together.
CREATE OR ALTER PROCEDURE dbo.ApplyCoinChange
 @UserId bigint, @Amount int, @Reason varchar(40), @IdempotencyKey varchar(150),
 @ReferenceType varchar(40)=NULL, @ReferenceId nvarchar(100)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Amount=0 OR NULLIF(@Reason,'') IS NULL OR NULLIF(@IdempotencyKey,'') IS NULL
  THROW 51001,'Invalid ledger request.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @Balance bigint, @Existing bigint;
  SELECT @Balance=Balance FROM dbo.Wallets WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@UserId;
  IF @Balance IS NULL THROW 51002,'Wallet not found.',1;
  SELECT @Existing=LedgerId FROM dbo.CoinLedger WITH(UPDLOCK,HOLDLOCK) WHERE IdempotencyKey=@IdempotencyKey;
  IF @Existing IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.CoinLedger WHERE LedgerId=@Existing AND UserId=@UserId AND Amount=@Amount AND Reason=@Reason
    AND ISNULL(ReferenceType,'')=ISNULL(@ReferenceType,'') AND ISNULL(ReferenceId,'')=ISNULL(@ReferenceId,''))
    THROW 51003,'Idempotency key conflicts with a different request.',1;
   COMMIT; RETURN;
  END;
  IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND Status='Active') THROW 51004,'User is not active.',1;
  SET @Balance=@Balance+CONVERT(bigint,@Amount);
  IF @Balance<0 OR @Balance>2147483647 THROW 51005,'Insufficient coins or balance overflow.',1;
  UPDATE dbo.Wallets SET Balance=CONVERT(int,@Balance) WHERE UserId=@UserId;
  INSERT dbo.CoinLedger(UserId,Amount,BalanceAfter,Reason,IdempotencyKey,ReferenceType,ReferenceId)
   VALUES(@UserId,@Amount,CONVERT(int,@Balance),@Reason,@IdempotencyKey,@ReferenceType,@ReferenceId);
  COMMIT;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK;
  THROW;
 END CATCH;
END;
GO
-- Call only AFTER backend validates signature, merchant, amount, currency and paid status.
-- Event metadata is not proof of payment by itself. The provider payload is verified outside SQL.
CREATE OR ALTER PROCEDURE dbo.ConfirmPayment
 @PaymentId bigint, @ProviderTransactionId varchar(150), @ProviderEventId varchar(150),
 @PaidAmountVnd int, @PayloadHash binary(32)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NULLIF(@ProviderTransactionId,'') IS NULL OR NULLIF(@ProviderEventId,'') IS NULL OR @PayloadHash IS NULL
  THROW 51006,'Missing verified payment identifiers.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @UserId bigint,@Coins int,@Price int,@Status varchar(20),@Provider varchar(30),@Txn varchar(150);
  SELECT @UserId=UserId,@Coins=Coins,@Price=PriceVnd,@Status=Status,@Provider=Provider,@Txn=ProviderTransactionId
   FROM dbo.Payments WITH(UPDLOCK,HOLDLOCK) WHERE PaymentId=@PaymentId;
  IF @UserId IS NULL OR @Price<>@PaidAmountVnd THROW 51007,'Payment not found or amount mismatch.',1;
  IF @Status='Paid'
  BEGIN
   IF @Txn<>@ProviderTransactionId THROW 51008,'Payment transaction mismatch.',1;
   COMMIT; RETURN;
  END;
  IF @Status NOT IN('Pending','Failed') THROW 51009,'Payment cannot transition to Paid.',1;
  INSERT dbo.PaymentEvents(PaymentId,Provider,ProviderEventId,SignatureVerified,PayloadHash,ProcessedAt)
   VALUES(@PaymentId,@Provider,@ProviderEventId,1,@PayloadHash,SYSUTCDATETIME());
  DECLARE @Key varchar(150)=CONCAT('payment:',@PaymentId),@Ref nvarchar(100)=CONVERT(nvarchar(100),@PaymentId);
  EXEC dbo.ApplyCoinChange @UserId,@Coins,'Purchase',@Key,'Payment',@Ref;
  UPDATE dbo.Payments SET Status='Paid',PaidAt=SYSUTCDATETIME(),ProviderTransactionId=@ProviderTransactionId,
   LedgerId=(SELECT LedgerId FROM dbo.CoinLedger WHERE IdempotencyKey=@Key) WHERE PaymentId=@PaymentId;
  COMMIT;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK;
  THROW;
 END CATCH;
END;
GO
CREATE OR ALTER VIEW dbo.vWalletReconciliation AS
 SELECT w.UserId,w.Balance,COALESCE(SUM(CONVERT(bigint,l.Amount)),0) AS LedgerBalance,
 CONVERT(bigint,w.Balance)-COALESCE(SUM(CONVERT(bigint,l.Amount)),0) AS Difference
 FROM dbo.Wallets w LEFT JOIN dbo.CoinLedger l ON l.UserId=w.UserId GROUP BY w.UserId,w.Balance;
GO
CREATE OR ALTER VIEW dbo.vOpenReportQueue AS
 SELECT r.ReportId,r.ContentId,r.SlideId,r.Reason,r.Status,r.AssignedTo,r.CreatedAt,r.DueAt,
 DATEDIFF(minute,SYSUTCDATETIME(),r.DueAt) AS MinutesRemaining
 FROM dbo.ContentReports r WHERE r.Status IN('Open','Investigating');
GO
CREATE OR ALTER VIEW dbo.vChapterReadiness AS
 SELECT e.UserId,e.CourseId,c.ChapterId,COUNT(DISTINCT cc.ConceptId) AS ConceptCount,
 CAST(COALESCE(AVG(COALESCE(p.Mastery,0)),0) AS decimal(5,2)) AS Readiness
 FROM dbo.Enrollments e JOIN dbo.Chapters c ON c.CourseId=e.CourseId
 LEFT JOIN (SELECT DISTINCT i.ChapterId,k.ConceptId FROM dbo.ContentItems i
 JOIN dbo.ContentConcepts k ON k.ContentId=i.ContentId WHERE i.Status='Published' AND i.OwnerUserId IS NULL) cc ON cc.ChapterId=c.ChapterId
 LEFT JOIN dbo.UserConceptProgress p ON p.UserId=e.UserId AND p.ConceptId=cc.ConceptId
 WHERE e.IsActive=1 GROUP BY e.UserId,e.CourseId,c.ChapterId;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=3) INSERT dbo.SchemaVersions(Version) VALUES(3);
GO
