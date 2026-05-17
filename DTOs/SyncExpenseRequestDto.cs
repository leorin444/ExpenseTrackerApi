using ExpenseTracker.API.DTOs;

public class SyncExpenseRequestDto
{
    public List<ExpenseSyncDto> Expenses { get; set; }
    public DateTime LastSyncTime { get; set; }  // for fetching server updates
}