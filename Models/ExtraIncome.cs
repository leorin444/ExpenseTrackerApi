using System;

namespace ExpenseTracker.API.Models
{
    public class ExtraIncome
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Source { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public bool IsDeleted { get; set; }
    }
}
