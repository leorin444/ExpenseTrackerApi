namespace ExpenseTracker.API.DTOs
{
    public class ExpenseSyncDto
    {
        public int? ServerId { get; set; }

        public string ClientExpenseId { get; set; }   // unique id from flutter

        public int CategoryId { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; }

        public DateTime ExpenseDate { get; set; }

        public string ReceiptImageBase64 { get; set; }

        public DateTime LastUpdatedTimestamp { get; set; }

        public bool IsDeleted { get; set; }
    }
}