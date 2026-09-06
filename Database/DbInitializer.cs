using Dapper;
using System.Data;

public class DbInitializer
{
    private readonly DbConnectionFactory _connectionFactory;

    public DbInitializer(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var sql = @"
            -- Users Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                FirebaseUid NVARCHAR(256),
                Email NVARCHAR(510),
                PasswordHash NVARCHAR(512),
                CreatedAt DATETIME DEFAULT GETUTCDATE(),
                LastLogin DATETIME2
            );

            -- RefreshTokens Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
            CREATE TABLE RefreshTokens (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT NOT NULL,
                Token NVARCHAR(1000) NOT NULL,
                ExpiresAt DATETIME NOT NULL,
                CreatedAt DATETIME DEFAULT GETUTCDATE()
            );

            -- PasswordResetTokens Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
            CREATE TABLE PasswordResetTokens (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Email NVARCHAR(510) NOT NULL,
                Token NVARCHAR(1000) NOT NULL,
                ExpiresAt DATETIME NOT NULL,
                CreatedAt DATETIME DEFAULT GETUTCDATE()
            );

            -- Budgets Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Budgets')
            CREATE TABLE Budgets (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT,
                MonthlyBudget DECIMAL(18,2),
                DailyLimit DECIMAL(18,2),
                CreatedAt DATETIME DEFAULT GETUTCDATE(),
                LastUpdated DATETIME2 DEFAULT GETUTCDATE()
            );

            -- Categories Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
            CREATE TABLE Categories (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(200),
                Icon NVARCHAR(100),
                Color NVARCHAR(40),
                CreatedAt DATETIME DEFAULT GETUTCDATE(),
                LastUpdated DATETIME2,
                Timestamp BIGINT,
                IsDeleted BIT DEFAULT 0
            );

            -- Expenses Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
            CREATE TABLE Expenses (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT,
                CategoryId INT,
                Amount DECIMAL(18,2),
                Note NVARCHAR(MAX),
                ExpenseDate DATETIME DEFAULT GETUTCDATE(),
                ImageBase64 NVARCHAR(MAX),
                LocalId NVARCHAR(200),
                ClientId NVARCHAR(200),
                ClientExpenseId NVARCHAR(200),
                CreatedAt DATETIME DEFAULT GETUTCDATE(),
                UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
                Timestamp DATETIME DEFAULT GETUTCDATE(),
                IsDeleted BIT DEFAULT 0
            );

            -- FinanceProfiles Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinanceProfiles')
            CREATE TABLE FinanceProfiles (
                UserId NVARCHAR(200) PRIMARY KEY,
                MonthlyIncome FLOAT,
                SavingsPercentage FLOAT,
                FixedExpenses FLOAT,
                Timestamp BIGINT,
                IsDeleted BIT DEFAULT 0
            );

            -- ExtraIncomes Table
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExtraIncomes')
            CREATE TABLE ExtraIncomes (
                Id NVARCHAR(100) PRIMARY KEY,
                Source NVARCHAR(510),
                Amount FLOAT,
                Date NVARCHAR(100),
                UserId NVARCHAR(200),
                Timestamp BIGINT,
                IsDeleted BIT DEFAULT 0
            );
        ";

        connection.Execute(sql);
    }
}
