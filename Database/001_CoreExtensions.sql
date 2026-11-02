-- Non-destructive additions for real saved results and grounded question history.
-- Existing 11 baseline tables in GameDB remain unchanged.
USE GameDB;
GO
IF OBJECT_ID(N'dbo.Saves', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Saves (
        SaveId INT IDENTITY PRIMARY KEY,
        UserId INT NOT NULL REFERENCES dbo.Users(UserId),
        RunId INT NOT NULL REFERENCES dbo.Runs(RunId),
        Title NVARCHAR(200) NOT NULL,
        SavedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Saves_User_Run UNIQUE (UserId, RunId)
    );
END;
IF OBJECT_ID(N'dbo.ScenarioMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScenarioMessages (
        MessageId INT IDENTITY PRIMARY KEY,
        UserId INT NOT NULL REFERENCES dbo.Users(UserId),
        ScenarioId INT NOT NULL REFERENCES dbo.Scenarios(ScenarioId),
        Question NVARCHAR(1000) NOT NULL,
        Answer NVARCHAR(MAX) NOT NULL,
        SourceSlide INT NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_ScenarioMessages_Owner ON dbo.ScenarioMessages(UserId, ScenarioId);
END;
GO
