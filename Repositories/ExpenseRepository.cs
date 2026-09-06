using Dapper;
using ExpenseTracker.API.DTOs;

public class ExpenseRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ExpenseRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SyncExpenseResponseDto> SyncExpenses(int userId, List<ExpenseSyncDto> clientExpenses, DateTime lastSyncTime)
    {
        using var db = _dbFactory.CreateConnection();
        var deletedIds = new List<int>();

        foreach (var e in clientExpenses)
        {
            if (e.ServerId.HasValue && e.ServerId.Value > 0)
            {
                // existing server record
                var server = await db.QueryFirstOrDefaultAsync<ExpenseSyncDto>(@"
                    SELECT 
                        Id AS ServerId, 
                        ISNULL(ClientExpenseId, ClientId) AS ClientExpenseId, 
                        CategoryId, 
                        Amount, 
                        Note AS Description, 
                        ExpenseDate, 
                        ImageBase64 AS ReceiptImageBase64, 
                        UpdatedAt AS LastUpdatedTimestamp, 
                        IsDeleted 
                    FROM Expenses 
                    WHERE Id=@Id AND UserId=@UserId",
                    new { Id = e.ServerId.Value, UserId = userId }
                );

                if (server != null)
                {
                    if (e.IsDeleted)
                    {
                        var updatedAt = e.LastUpdatedTimestamp == default ? DateTime.UtcNow : e.LastUpdatedTimestamp;
                        await db.ExecuteAsync(
                            "UPDATE Expenses SET IsDeleted=1, UpdatedAt=@UpdatedAt, [Timestamp]=DATEDIFF_BIG(MILLISECOND,'1970-01-01 00:00:00',GETUTCDATE()) WHERE Id=@Id",
                            new { UpdatedAt = updatedAt, Id = e.ServerId.Value }
                        );
                        deletedIds.Add(e.ServerId.Value);
                    }
                    else if (e.LastUpdatedTimestamp > server.LastUpdatedTimestamp)
                    {
                        // update server
                        await db.ExecuteAsync(@"
                            UPDATE Expenses
                            SET CategoryId=@CategoryId,
                                Amount=@Amount,
                                Note=@Note,
                                ExpenseDate=@ExpenseDate,
                                ImageBase64=@ImageBase64,
                                UpdatedAt=@UpdatedAt,
                                [Timestamp]=DATEDIFF_BIG(MILLISECOND,'1970-01-01 00:00:00',GETUTCDATE())
                            WHERE Id=@Id
                        ", new
                        {
                            CategoryId = e.CategoryId,
                            Amount = e.Amount,
                            Note = e.Description,
                            ExpenseDate = e.ExpenseDate,
                            ImageBase64 = e.ReceiptImageBase64,
                            UpdatedAt = e.LastUpdatedTimestamp == default ? DateTime.UtcNow : e.LastUpdatedTimestamp,
                            Id = e.ServerId.Value
                        });
                    }
                }
            }
            else
            {
                // new record → insert
                var updatedAt = e.LastUpdatedTimestamp == default ? DateTime.UtcNow : e.LastUpdatedTimestamp;
                var newId = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Expenses (UserId, ClientId, ClientExpenseId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES (@UserId, @ClientId, @ClientExpenseId, @CategoryId, @Amount, @Note, @ExpenseDate, @ImageBase64, GETUTCDATE(), @UpdatedAt, @IsDeleted);
                    SELECT CAST(SCOPE_IDENTITY() as int);
                ", new
                {
                    UserId = userId,
                    ClientId = e.ClientExpenseId,
                    ClientExpenseId = e.ClientExpenseId,
                    CategoryId = e.CategoryId,
                    Amount = e.Amount,
                    Note = e.Description,
                    ExpenseDate = e.ExpenseDate,
                    ImageBase64 = e.ReceiptImageBase64,
                    UpdatedAt = updatedAt,
                    IsDeleted = e.IsDeleted ? 1 : 0
                });

                e.ServerId = newId; // return serverId to Flutter
            }
        }

        // fetch server updates since last sync
        var serverUpdates = await db.QueryAsync<ExpenseSyncDto>(@"
            SELECT 
                Id AS ServerId, 
                ISNULL(ClientExpenseId, ClientId) AS ClientExpenseId, 
                CategoryId, 
                Amount, 
                Note AS Description, 
                ExpenseDate, 
                ImageBase64 AS ReceiptImageBase64, 
                UpdatedAt AS LastUpdatedTimestamp, 
                IsDeleted
            FROM Expenses
            WHERE UserId=@UserId AND UpdatedAt>@LastSyncTime
        ", new { UserId = userId, LastSyncTime = lastSyncTime });

        return new SyncExpenseResponseDto
        {
            ServerTime = DateTime.UtcNow,
            Expenses = serverUpdates.ToList(),
            DeletedExpenseIds = deletedIds
        };
    }

    public async Task<int> AddExpenseAsync(ExpenseDto expense)
    {
        using var db = _dbFactory.CreateConnection();
        var newId = await db.ExecuteScalarAsync<int>(@"
            IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = @CategoryId)
                SET @CategoryId = 1;

            DECLARE @ExistingId INT;
            IF (@Id > 0)
            BEGIN
                SELECT @ExistingId = Id FROM Expenses WHERE Id = @Id;
            END

            IF (@ExistingId IS NULL)
            BEGIN
                SELECT TOP 1 @ExistingId = Id 
                FROM Expenses 
                WHERE UserId = @UserId 
                  AND Amount = @Amount 
                  AND CategoryId = @CategoryId
                  AND CAST(ExpenseDate AS DATE) = CAST(@ExpenseDate AS DATE)
                  AND ISNULL(Note, '') = ISNULL(@Note, '')
                  AND IsDeleted = 0;
            END

            IF (@ExistingId IS NOT NULL)
            BEGIN
                UPDATE Expenses 
                SET CategoryId = @CategoryId, 
                    Amount = @Amount, 
                    Note = @Note, 
                    ExpenseDate = @ExpenseDate, 
                    ImageBase64 = COALESCE(@ImageBase64, ImageBase64), 
                    UpdatedAt = GETUTCDATE(),
                    [Timestamp] = DATEDIFF_BIG(MILLISECOND, '1970-01-01 00:00:00', GETUTCDATE())
                WHERE Id = @ExistingId;
                SELECT @ExistingId;
            END
            ELSE
            BEGIN
                INSERT INTO Expenses (UserId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, CreatedAt, UpdatedAt, IsDeleted, [Timestamp])
                VALUES (@UserId, @CategoryId, @Amount, @Note, @ExpenseDate, @ImageBase64, GETUTCDATE(), GETUTCDATE(), 0,
                        DATEDIFF_BIG(MILLISECOND, '1970-01-01 00:00:00', GETUTCDATE()));
                SELECT CAST(SCOPE_IDENTITY() as int);
            END
        ", expense);
        return newId;
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(int id)
    {
        using var db = _dbFactory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<ExpenseDto>(
            "SELECT * FROM Expenses WHERE Id=@Id AND IsDeleted=0", new { Id = id });
    }

    public async Task<bool> UpdateExpenseAsync(ExpenseDto expense)
    {
        using var db = _dbFactory.CreateConnection();
        var rows = await db.ExecuteAsync(@"
        UPDATE Expenses
        SET CategoryId=@CategoryId,
            Amount=@Amount,
            Note=@Note,
            ExpenseDate=@ExpenseDate,
            ImageBase64=@ImageBase64,
            UpdatedAt=GETUTCDATE(),
            [Timestamp]=DATEDIFF_BIG(MILLISECOND, '1970-01-01 00:00:00', GETUTCDATE())
        WHERE Id=@Id AND IsDeleted=0
    ", expense);
        return rows > 0;
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        using var db = _dbFactory.CreateConnection();
        var rows = await db.ExecuteAsync(
            "UPDATE Expenses SET IsDeleted=1, UpdatedAt=GETUTCDATE() WHERE Id=@Id", new { Id = id });
        return rows > 0;
    }

    public async Task<IEnumerable<ExpenseDto>> GetExpensesByUserAsync(int userId)
    {
        using var db = _dbFactory.CreateConnection();
        var expenses = await db.QueryAsync<ExpenseDto>(@"
            SELECT 
                e.Id,
                e.UserId,
                e.CategoryId,
                e.Amount,
                e.Note,
                e.ExpenseDate,
                e.ImageBase64,
                ISNULL(e.ClientExpenseId, e.ClientId) AS ClientExpenseId,
                c.Name AS CategoryName
            FROM Expenses e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.UserId=@UserId AND e.IsDeleted=0 
            ORDER BY e.ExpenseDate DESC",
            new { UserId = userId }
        );
        return expenses;
    }

    public async Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
    {
        using var db = _dbFactory.CreateConnection();
        var expenses = await db.QueryAsync<ExpenseDto>(@"
            SELECT 
                e.Id,
                e.UserId,
                e.CategoryId,
                e.Amount,
                e.Note,
                e.ExpenseDate,
                e.ImageBase64,
                ISNULL(e.ClientExpenseId, e.ClientId) AS ClientExpenseId,
                c.Name AS CategoryName
            FROM Expenses e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.IsDeleted=0 
            ORDER BY e.ExpenseDate DESC"
        );
        return expenses;
    }
}