-- Fresh database only. Do not run against existing tables. No seed accounts or demo passwords.
IF DB_ID(N'GameDB') IS NULL
BEGIN
    CREATE DATABASE GameDB;
END
GO

USE GameDB;
GO

-- =========================================================
-- 1. USERS
-- =========================================================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role INT NOT NULL DEFAULT 1,           -- 1 = User, 2 = Admin
    Coin INT NOT NULL DEFAULT 20,          -- coin tặng ban đầu
    IsLocked BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_Users_Role CHECK (Role IN (1,2)),
    CONSTRAINT CK_Users_Coin CHECK (Coin >= 0)
);
GO

-- =========================================================
-- 2. FILES
-- Lưu metadata PDF. File thật lưu ngoài storage/server.
-- =========================================================
CREATE TABLE Files (
    FileId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    TotalPages INT NOT NULL,
    Language NVARCHAR(20) NULL,
    UploadedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Files_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- =========================================================
-- 3. SLIDES
-- Mỗi trang PDF là 1 slide
-- =========================================================
CREATE TABLE Slides (
    SlideId INT IDENTITY(1,1) PRIMARY KEY,
    FileId INT NOT NULL,
    SlideNumber INT NOT NULL,
    SlideText NVARCHAR(MAX) NULL,
    ImagePath NVARCHAR(500) NULL,

    CONSTRAINT FK_Slides_Files
        FOREIGN KEY (FileId) REFERENCES Files(FileId)
        ON DELETE CASCADE,

    CONSTRAINT UQ_Slides_File_Slide UNIQUE (FileId, SlideNumber)
);
GO

-- =========================================================
-- 4. SCENARIOS
-- Sa bàn AI sinh từ PDF
-- =========================================================
CREATE TABLE Scenarios (
    ScenarioId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    FileId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    RoleName NVARCHAR(150) NULL,
    CompanyName NVARCHAR(150) NULL,
    Description NVARCHAR(MAX) NULL,
    ScenarioJson NVARCHAR(MAX) NULL,
    AiModel NVARCHAR(100) NULL,
    CoinCost INT NOT NULL DEFAULT 3,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Scenarios_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),

    CONSTRAINT FK_Scenarios_Files
        FOREIGN KEY (FileId) REFERENCES Files(FileId),

    CONSTRAINT CK_Scenarios_CoinCost CHECK (CoinCost >= 0)
);
GO

-- =========================================================
-- 5. RUNS
-- Một lần User chơi một Scenario
-- =========================================================
CREATE TABLE Runs (
    RunId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ScenarioId INT NOT NULL,
    Score DECIMAL(5,2) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Playing',
    Rating INT NULL,
    StartedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CompletedAt DATETIME NULL,

    CONSTRAINT FK_Runs_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),

    CONSTRAINT FK_Runs_Scenarios
        FOREIGN KEY (ScenarioId) REFERENCES Scenarios(ScenarioId),

    CONSTRAINT CK_Runs_Status CHECK (Status IN (N'Playing', N'Completed')),
    CONSTRAINT CK_Runs_Rating CHECK (Rating IS NULL OR Rating BETWEEN 1 AND 5)
);
GO

-- =========================================================
-- 6. RUN_CHOICES
-- Lưu lựa chọn A/B/C trong từng tuần / câu
-- =========================================================
CREATE TABLE RunChoices (
    RunChoiceId INT IDENTITY(1,1) PRIMARY KEY,
    RunId INT NOT NULL,
    StepNumber INT NOT NULL,
    Choice CHAR(1) NOT NULL,
    ChoiceText NVARCHAR(500) NULL,
    ResultText NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_RunChoices_Runs
        FOREIGN KEY (RunId) REFERENCES Runs(RunId)
        ON DELETE CASCADE,

    CONSTRAINT CK_RunChoices_Choice CHECK (Choice IN ('A','B','C'))
);
GO

-- =========================================================
-- 7. DEEP_PRACTICES
-- Lưu phiên Học sâu
-- =========================================================
CREATE TABLE DeepPractices (
    DeepPracticeId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ScenarioId INT NOT NULL,
    TotalQuestions INT NOT NULL DEFAULT 0,
    CorrectAnswers INT NOT NULL DEFAULT 0,
    CoinCost INT NOT NULL DEFAULT 0,
    ResultJson NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_DeepPractices_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),

    CONSTRAINT FK_DeepPractices_Scenarios
        FOREIGN KEY (ScenarioId) REFERENCES Scenarios(ScenarioId),

    CONSTRAINT CK_DeepPractices_Count
        CHECK (CorrectAnswers >= 0 AND TotalQuestions >= CorrectAnswers),

    CONSTRAINT CK_DeepPractices_CoinCost
        CHECK (CoinCost >= 0)
);
GO

-- =========================================================
-- 8. COIN_TRANSACTIONS
-- Sổ cộng / trừ coin
-- =========================================================
CREATE TABLE CoinTransactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Amount INT NOT NULL,                     -- + cộng coin, - trừ coin
    TransactionType NVARCHAR(30) NOT NULL,   -- Purchase, Build, AI, Reward, Refund...
    Description NVARCHAR(255) NULL,
    BalanceAfter INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_CoinTransactions_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- =========================================================
-- 9. PAYMENTS
-- Giao dịch mua coin
-- =========================================================
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    PackageName NVARCHAR(50) NOT NULL,
    AmountVND INT NOT NULL,
    CoinAmount INT NOT NULL,
    PaymentMethod NVARCHAR(30) NULL,     -- VNPay / MoMo / ZaloPay
    Status NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Payments_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),

    CONSTRAINT CK_Payments_Status
        CHECK (Status IN (N'Pending', N'Paid', N'Failed', N'Cancelled'))
);
GO

-- =========================================================
-- 10. AI_CALLS
-- Theo dõi số lần gọi AI và chi phí
-- =========================================================
CREATE TABLE AICalls (
    AICallId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    FeatureName NVARCHAR(50) NOT NULL,     -- Build / AskAI / Deep / Lookup...
    AiProvider NVARCHAR(50) NULL,          -- Gemini / OpenAI / DeepSeek
    AiModel NVARCHAR(100) NULL,
    InputTokens INT NOT NULL DEFAULT 0,
    OutputTokens INT NOT NULL DEFAULT 0,
    CostUSD DECIMAL(18,6) NOT NULL DEFAULT 0,
    CoinCharged INT NOT NULL DEFAULT 0,
    IsSuccess BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_AICalls_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- =========================================================
-- 11. ADMIN_LOGS
-- Ghi lại các thao tác Admin
-- =========================================================
CREATE TABLE AdminLogs (
    AdminLogId INT IDENTITY(1,1) PRIMARY KEY,
    AdminUserId INT NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    TargetUserId INT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_AdminLogs_Admin
        FOREIGN KEY (AdminUserId) REFERENCES Users(UserId),

    CONSTRAINT FK_AdminLogs_TargetUser
        FOREIGN KEY (TargetUserId) REFERENCES Users(UserId)
);
GO


-- =========================================================
-- INDEX
-- =========================================================
CREATE INDEX IX_Files_UserId ON Files(UserId);
CREATE INDEX IX_Scenarios_UserId ON Scenarios(UserId);
CREATE INDEX IX_Runs_UserId ON Runs(UserId);
CREATE INDEX IX_CoinTransactions_UserId ON CoinTransactions(UserId);
CREATE INDEX IX_Payments_UserId ON Payments(UserId);
CREATE INDEX IX_AICalls_UserId ON AICalls(UserId);
GO


-- =========================================================

