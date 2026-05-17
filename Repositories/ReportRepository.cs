using Dapper;
using System.Data;

public class ReportRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ReportRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Monthly report
    public async Task<IEnumerable<dynamic>> GetMonthlyReport(int userId, int year)
    {
        using var db = _dbFactory.CreateConnection();

        return await db.QueryAsync(
            "sp_GetMonthlyExpenseReport",
            new { UserId = userId, Year = year },
            commandType: CommandType.StoredProcedure
        );
    }

    // Category report
    public async Task<IEnumerable<dynamic>> GetCategoryReport(int userId, int month, int year)
    {
        using var db = _dbFactory.CreateConnection();

        return await db.QueryAsync(
            "sp_GetCategoryExpenseReport",
            new { UserId = userId, Month = month, Year = year },
            commandType: CommandType.StoredProcedure
        );
    }

    // Daily report
    public async Task<IEnumerable<dynamic>> GetDailyReport(int userId, int month, int year)
    {
        using var db = _dbFactory.CreateConnection();

        return await db.QueryAsync(
            "sp_GetDailyExpenseReport",
            new { UserId = userId, Month = month, Year = year },
            commandType: CommandType.StoredProcedure
        );
    }

    // Budget report
    public async Task<dynamic> GetBudgetReport(int userId, int month, int year)
    {
        using var db = _dbFactory.CreateConnection();

        return await db.QueryFirstOrDefaultAsync(
            "sp_GetBudgetUsageReport",
            new { UserId = userId, Month = month, Year = year },
            commandType: CommandType.StoredProcedure
        );
    }

    // Year summary
    public async Task<dynamic> GetYearReport(int userId, int year)
    {
        using var db = _dbFactory.CreateConnection();

        return await db.QueryFirstOrDefaultAsync(
            "sp_GetYearExpenseSummary",
            new { UserId = userId, Year = year },
            commandType: CommandType.StoredProcedure
        );
    }
}