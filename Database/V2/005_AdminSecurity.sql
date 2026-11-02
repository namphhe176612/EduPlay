USE EduPlayV2;
GO
IF COL_LENGTH('dbo.AdminSecurity','LastTotpCounter') IS NULL ALTER TABLE dbo.AdminSecurity ADD LastTotpCounter bigint NOT NULL DEFAULT -1;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=5) INSERT dbo.SchemaVersions(Version) VALUES(5);
GO
