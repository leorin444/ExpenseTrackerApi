namespace ExpenseTracker.API.Models
{
    public class FinanceProfile
    {
        public string UserId { get; set; } = string.Empty;
        public double MonthlyIncome { get; set; }
        public double SavingsPercentage { get; set; }
        public double FixedExpenses { get; set; }
        public long Timestamp { get; set; }
        public bool IsDeleted { get; set; }
    }
}
