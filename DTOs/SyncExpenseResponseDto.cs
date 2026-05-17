// DTOs/SyncExpenseResponseDto.cs
namespace ExpenseTracker.API.DTOs
{
    public class SyncExpenseResponseDto
    {
        public DateTime ServerTime { get; set; }
        public List<ExpenseSyncDto> Expenses { get; set; }
        public List<int> DeletedExpenseIds { get; set; }
    }
}