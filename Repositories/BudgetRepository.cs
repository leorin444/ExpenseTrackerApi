using Dapper;

public class BudgetRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public BudgetRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<BudgetDto> GetUserBudget(int userId)
    {
        using var db = _dbFactory.CreateConnection();

        var budget = await db.QueryFirstOrDefaultAsync<BudgetDto>(
            "SELECT MonthlyLimit, DailyLimit FROM Budgets WHERE UserId=@UserId",
            new { UserId = userId }
        );

        return budget ?? new BudgetDto { MonthlyLimit = 0, DailyLimit = 0 };
    }
}