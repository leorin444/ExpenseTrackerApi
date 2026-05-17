namespace ExpenseTracker.API.DTOs
{
    public class ExpenseDto
    {
        public int Id { get; set; }             // Primary key
        public int UserId { get; set; }         // Owner of the expense
        public int CategoryId { get; set; }     // Category reference
        public decimal Amount { get; set; }     // Expense amount
        public string Note { get; set; }        // Description / note
        public DateTime ExpenseDate { get; set; } // When the expense occurred
        public string? ImageBase64 { get; set; }  // Optional receipt image
    }
}
