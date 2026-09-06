-- ============================================================================
-- Expense Tracker Database Setup Script
-- SQL Server Database Schema & Stored Procedures
-- ============================================================================

USE [expense_tracker];
GO

-- 1. Users Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirebaseUid NVARCHAR(200) NULL,
        Email NVARCHAR(510) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Role NVARCHAR(50) DEFAULT 'User',
        CreatedAt DATETIME DEFAULT GETUTCDATE(),
        LastLogin DATETIME2 NULL
    );
END
GO

-- 2. Categories Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Categories' AND xtype='U')
BEGIN
    CREATE TABLE Categories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Icon NVARCHAR(100) NULL,
        Color NVARCHAR(40) NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE(),
        LastUpdated DATETIME2 NULL,
        Timestamp BIGINT DEFAULT 0,
        IsDeleted BIT DEFAULT 0
    );
END
GO

-- Seed Default Categories if Empty
IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (Name, Icon, Color, Timestamp, IsDeleted) VALUES
    ('Food', 'restaurant', '#FF5733', 0, 0),
    ('Transport', 'directions_car', '#3498DB', 0, 0),
    ('Shopping', 'shopping_cart', '#9B59B6', 0, 0),
    ('Bills', 'receipt', '#82597b', 0, 0),
    ('Entertainment', 'movie', '#2ECC71', 0, 0);
END
GO

-- 3. Expenses Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Expenses' AND xtype='U')
BEGIN
    CREATE TABLE Expenses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        CategoryId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        ExpenseDate DATETIME NOT NULL,
        Timestamp DATETIME NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE(),
        LocalId NVARCHAR(100) NULL,
        UpdatedAt DATETIME2 NULL,
        IsDeleted BIT DEFAULT 0,
        ClientId NVARCHAR(200) NULL,
        ClientExpenseId NVARCHAR(200) NULL,
        Note NVARCHAR(1000) NULL,
        ImageBase64 NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_Expenses_UserId ON Expenses(UserId);
    CREATE INDEX IX_Expenses_ExpenseDate ON Expenses(ExpenseDate);
END
GO

-- 4. Budgets Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Budgets' AND xtype='U')
BEGIN
    CREATE TABLE Budgets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        MonthlyBudget DECIMAL(18,2) NOT NULL DEFAULT 0,
        DailyLimit DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETUTCDATE(),
        LastUpdated DATETIME2 NULL
    );
    CREATE INDEX IX_Budgets_UserId ON Budgets(UserId);
END
GO

-- 5. RefreshTokens Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RefreshTokens' AND xtype='U')
BEGIN
    CREATE TABLE RefreshTokens (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Token NVARCHAR(1000) NOT NULL,
        ExpiresAt DATETIME NOT NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
END
GO

-- 6. PasswordResetTokens Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PasswordResetTokens' AND xtype='U')
BEGIN
    CREATE TABLE PasswordResetTokens (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(510) NOT NULL,
        Token NVARCHAR(1000) NOT NULL,
        ExpiresAt DATETIME NOT NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_PasswordResetTokens_Email ON PasswordResetTokens(Email);
END
GO

-- 7. FinanceProfiles Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FinanceProfiles' AND xtype='U')
BEGIN
    CREATE TABLE FinanceProfiles (
        UserId NVARCHAR(200) PRIMARY KEY,
        MonthlyIncome FLOAT NOT NULL DEFAULT 0,
        SavingsPercentage FLOAT NOT NULL DEFAULT 0,
        FixedExpenses FLOAT NOT NULL DEFAULT 0,
        Timestamp BIGINT NOT NULL DEFAULT 0,
        IsDeleted BIT NOT NULL DEFAULT 0
    );
END
GO

-- 8. ExtraIncomes Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExtraIncomes' AND xtype='U')
BEGIN
    CREATE TABLE ExtraIncomes (
        Id NVARCHAR(100) PRIMARY KEY,
        Source NVARCHAR(510) NOT NULL,
        Amount FLOAT NOT NULL DEFAULT 0,
        Date NVARCHAR(100) NULL,
        UserId NVARCHAR(200) NOT NULL,
        Timestamp BIGINT NOT NULL DEFAULT 0,
        IsDeleted BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_ExtraIncomes_UserId ON ExtraIncomes(UserId);
END
GO

-- 9. Table-Valued Parameter Type: ExpenseSyncType
-- Note: In SQL Server, a table type cannot be altered. If recreating with new columns (e.g. IsDeleted),
-- any referencing procedure must be dropped first, then the type dropped and recreated.
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_SyncExpenses')
    DROP PROCEDURE sp_SyncExpenses;
GO

IF EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'ExpenseSyncType')
    DROP TYPE ExpenseSyncType;
GO

CREATE TYPE ExpenseSyncType AS TABLE (
    ClientExpenseId NVARCHAR(100),
    CategoryId INT,
    Amount DECIMAL(18,2),
    Description NVARCHAR(500),
    ExpenseDate DATETIME,
    ReceiptImageBase64 NVARCHAR(MAX),
    LastUpdatedTimestamp DATETIME,
    IsDeleted BIT
);
GO

-- 10. Stored Procedure: sp_SyncExpenses
CREATE PROCEDURE sp_SyncExpenses
    @UserId INT,
    @LastSyncTime DATETIME,
    @IncomingExpenses ExpenseSyncType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ServerTime DATETIME = GETUTCDATE();

    -- Upsert incoming records
    MERGE Expenses AS target
    USING @IncomingExpenses AS source
    ON (target.UserId = @UserId AND target.ClientExpenseId = source.ClientExpenseId)
    WHEN MATCHED AND (source.LastUpdatedTimestamp > target.UpdatedAt OR target.UpdatedAt IS NULL) THEN
        UPDATE SET 
            CategoryId = source.CategoryId,
            Amount = source.Amount,
            Note = source.Description,
            ExpenseDate = source.ExpenseDate,
            ImageBase64 = source.ReceiptImageBase64,
            UpdatedAt = source.LastUpdatedTimestamp,
            IsDeleted = ISNULL(source.IsDeleted, 0)
    WHEN NOT MATCHED THEN
        INSERT (UserId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, ClientExpenseId, CreatedAt, UpdatedAt, IsDeleted)
        VALUES (@UserId, source.CategoryId, source.Amount, source.Description, source.ExpenseDate, source.ReceiptImageBase64, source.ClientExpenseId, @ServerTime, source.LastUpdatedTimestamp, ISNULL(source.IsDeleted, 0));

    -- Return expenses changed since last sync
    SELECT 
        Id AS ServerId,
        ClientExpenseId,
        CategoryId,
        Amount,
        Note AS Description,
        ExpenseDate,
        ImageBase64 AS ReceiptImageBase64,
        UpdatedAt AS LastUpdatedTimestamp,
        IsDeleted
    FROM Expenses
    WHERE UserId = @UserId AND (UpdatedAt >= @LastSyncTime OR CreatedAt >= @LastSyncTime);
END
GO

-- 11. Stored Procedure: sp_GetMonthlyExpenseReport
CREATE OR ALTER PROCEDURE sp_GetMonthlyExpenseReport
    @UserId INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        MONTH(ExpenseDate) AS MonthNumber,
        DATENAME(month, ExpenseDate) AS MonthName,
        SUM(Amount) AS TotalAmount
    FROM Expenses
    WHERE UserId = @UserId 
      AND YEAR(ExpenseDate) = @Year 
      AND IsDeleted = 0
    GROUP BY MONTH(ExpenseDate), DATENAME(month, ExpenseDate)
    ORDER BY MonthNumber;
END
GO

-- 12. Stored Procedure: sp_GetCategoryExpenseReport
CREATE OR ALTER PROCEDURE sp_GetCategoryExpenseReport
    @UserId INT,
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        c.Name AS CategoryName,
        c.Icon AS CategoryIcon,
        c.Color AS CategoryColor,
        SUM(e.Amount) AS TotalAmount
    FROM Expenses e
    LEFT JOIN Categories c ON e.CategoryId = c.Id
    WHERE e.UserId = @UserId 
      AND MONTH(e.ExpenseDate) = @Month 
      AND YEAR(e.ExpenseDate) = @Year 
      AND e.IsDeleted = 0
    GROUP BY c.Name, c.Icon, c.Color;
END
GO

-- 13. Stored Procedure: sp_GetDailyExpenseReport
CREATE OR ALTER PROCEDURE sp_GetDailyExpenseReport
    @UserId INT,
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        CAST(ExpenseDate AS DATE) AS [Date],
        SUM(Amount) AS TotalAmount
    FROM Expenses
    WHERE UserId = @UserId 
      AND MONTH(ExpenseDate) = @Month 
      AND YEAR(ExpenseDate) = @Year 
      AND IsDeleted = 0
    GROUP BY CAST(ExpenseDate AS DATE)
    ORDER BY [Date];
END
GO

-- 14. Stored Procedure: sp_GetBudgetUsageReport
CREATE OR ALTER PROCEDURE sp_GetBudgetUsageReport
    @UserId INT,
    @Month INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Budget DECIMAL(18,2) = 0;
    DECLARE @Spent DECIMAL(18,2) = 0;

    SELECT @Budget = ISNULL(MonthlyBudget, 0) FROM Budgets WHERE UserId = @UserId;
    
    SELECT @Spent = ISNULL(SUM(Amount), 0) 
    FROM Expenses 
    WHERE UserId = @UserId 
      AND MONTH(ExpenseDate) = @Month 
      AND YEAR(ExpenseDate) = @Year 
      AND IsDeleted = 0;

    SELECT 
        @Budget AS Budget,
        @Spent AS Spent,
        (@Budget - @Spent) AS Remaining;
END
GO

-- 15. Stored Procedure: sp_GetYearExpenseSummary
CREATE OR ALTER PROCEDURE sp_GetYearExpenseSummary
    @UserId INT,
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH MonthlyTotals AS (
        SELECT MONTH(ExpenseDate) AS [Month], SUM(Amount) AS Total
        FROM Expenses
        WHERE UserId = @UserId AND YEAR(ExpenseDate) = @Year AND IsDeleted = 0
        GROUP BY MONTH(ExpenseDate)
    )
    SELECT 
        ISNULL(SUM(Total), 0) AS TotalYearExpense,
        ISNULL(AVG(Total), 0) AS AvgMonthlyExpense,
        ISNULL(MAX(Total), 0) AS HighestMonthExpense,
        ISNULL(MIN(Total), 0) AS LowestMonthExpense
    FROM MonthlyTotals;
END
GO
