USE EduPlayV2;
GO
SET XACT_ABORT ON;
BEGIN TRAN;
IF EXISTS(SELECT 1 FROM dbo.Users WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedEmail=UPPER(N'demo.admin@example.com'))
 THROW 50001,N'Email already exists; no account was changed.',1;
DECLARE @role int=(SELECT RoleId FROM dbo.Roles WHERE Code='SuperAdmin');
IF @role IS NULL THROW 50002,N'SuperAdmin role is missing.',1;
INSERT dbo.Users(FullName,Email,PasswordHash,ReferralCode,TermsVersion,TermsAcceptedAt,Status)
VALUES(N'Admin Demo',N'demo.admin@example.com',N'AQAAAAIAAYagAAAAEGr4HCFPFGIZUXRtLsm+cC9YQmWWBdsppdTsZO8Pn9HXmwSu6/+89KImspmzDT0FLA==',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),'2026-10',SYSUTCDATETIME(),'Active');
DECLARE @id bigint=SCOPE_IDENTITY();
INSERT dbo.Wallets(UserId) VALUES(@id);
INSERT dbo.UserSettings(UserId) VALUES(@id);
INSERT dbo.VaultSlots(UserId,SlotNumber) VALUES(@id,1),(@id,2),(@id,3);
INSERT dbo.UserRoles(UserId,RoleId) VALUES(@id,@role);
INSERT dbo.AdminSecurity(UserId,EncryptedTotpSecret,MfaEnabledAt,LastTotpCounter)
VALUES(@id,0x09F0C9F02FA99B2307E4AB4798D70538A20ABBD5E4BA84F6CF1863CED6F92E0C885E61CCDA75CA3E6662E1D3FE807E4B311C9F4C267E3C65AC69EC4CFDBC3F22D4A0CFADCBE581169C918F05F783AC3AFA22FD906164AE01BA40609CD7622EA3E56FCB3A735E27A57EFED0A108CB2B0479312877,SYSUTCDATETIME(),-1);
INSERT dbo.AdminAudit(ActorUserId,Action) VALUES(@id,'AdminProvisionedBySql');
COMMIT;
SELECT UserId,FullName,Email,Status FROM dbo.Users WHERE UserId=@id;
GO