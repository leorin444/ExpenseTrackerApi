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
            if (e.ServerId.HasValue)
            {
                // existing server record
                var server = await db.QueryFirstOrDefaultAsync<ExpenseSyncDto>(
                    "SELECT * FROM Expenses WHERE Id=@Id AND UserId=@UserId",
                    new { Id = e.ServerId.Value, UserId = userId }
                );

                if (server != null)
                {
                    if (e.IsDeleted)
                    {
                        await db.ExecuteAsync(
                            "UPDATE Expenses SET IsDeleted=1, UpdatedAt=@UpdatedAt WHERE Id=@Id",
                            new { UpdatedAt = e.LastUpdatedTimestamp, Id = e.ServerId.Value }
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
                                UpdatedAt=@UpdatedAt
                            WHERE Id=@Id
                        ", new
                        {
                            e.CategoryId,
                            e.Amount,
                            e.Description,
                            e.ExpenseDate,
                            e.ReceiptImageBase64,
                            e.LastUpdatedTimestamp,
                            Id = e.ServerId.Value
                        });
                    }
                }
            }
            else
            {
                // new record → insert
                var newId = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Expenses (UserId, ClientId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES (@UserId, @ClientId, @CategoryId, @Amount, @Note, @ExpenseDate, @ImageBase64, GETUTCDATE(), @UpdatedAt, @IsDeleted);
                    SELECT CAST(SCOPE_IDENTITY() as int);
                ", new
                {
                    UserId = userId,
                    e.ClientExpenseId,
                    e.CategoryId,
                    e.Amount,
                    e.Description,
                    e.ExpenseDate,
                    e.ReceiptImageBase64,
                    e.LastUpdatedTimestamp,
                    e.IsDeleted
                });

                e.ServerId = newId; // return serverId to Flutter
            }
        }

        // fetch server updates since last sync
        var serverUpdates = await db.QueryAsync<ExpenseSyncDto>(@"
            SELECT Id AS ServerId, ClientId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, UpdatedAt, IsDeleted
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
        INSERT INTO Expenses (UserId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, CreatedAt, UpdatedAt, IsDeleted)
        VALUES (@UserId, @CategoryId, @Amount, @Note, @ExpenseDate, @ImageBase64, GETUTCDATE(), GETUTCDATE(), 0);
        SELECT CAST(SCOPE_IDENTITY() as int);
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
            UpdatedAt=GETUTCDATE()
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
        var expenses = await db.QueryAsync<ExpenseDto>(
            "SELECT * FROM Expenses WHERE UserId=@UserId AND IsDeleted=0 ORDER BY ExpenseDate DESC",
            new { UserId = userId }
        );
        return expenses;
    }


}