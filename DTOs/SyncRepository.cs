using Dapper;
using ExpenseTracker.API.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

public class SyncRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public SyncRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SyncExpenses(int userId, List<ExpenseSyncDto> expenses)
    {
        using var connection = _dbFactory.CreateConnection();

        var table = new DataTable();

        table.Columns.Add("ClientExpenseId", typeof(string));
        table.Columns.Add("CategoryId", typeof(int));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("ReceiptImageBase64", typeof(string));
        table.Columns.Add("ExpenseDate", typeof(DateTime));
        table.Columns.Add("LastUpdatedTimestamp", typeof(DateTime));

        foreach (var e in expenses)
        {
            table.Rows.Add(
                e.ClientExpenseId,
                e.CategoryId,
                e.Amount,
                e.Description,
                e.ReceiptImageBase64,
                e.ExpenseDate,
                e.LastUpdatedTimestamp
            );
        }

        var parameters = new DynamicParameters();

        parameters.Add("@UserId", userId);

        parameters.Add(
            "@Expenses",
            table.AsTableValuedParameter("ExpenseSyncType")
        );

        await connection.ExecuteAsync(
            "sp_SyncExpenses",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}