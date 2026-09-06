using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseTracker.API.DTOs
{
    public class ExpenseDto
    {
        public int Id { get; set; }             // Primary key
        
        [JsonIgnore]
        public int UserId { get; set; }         // Owner of the expense (integer DB id)

        [JsonPropertyName("userId")]
        public JsonElement? RawUserId
        {
            get => null;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value.ValueKind == JsonValueKind.Number)
                    {
                        UserId = value.Value.GetInt32();
                    }
                    else if (value.Value.ValueKind == JsonValueKind.String)
                    {
                        var str = value.Value.GetString();
                        if (int.TryParse(str, out int parsed))
                            UserId = parsed;
                        else
                            FirebaseUid = str;
                    }
                }
            }
        }

        public string? FirebaseUid { get; set; }// Firebase UID — used when UserId is not yet known
        public int CategoryId { get; set; }     // Category reference
        public string? CategoryName { get; set; } // Category display name
        public string? ClientExpenseId { get; set; } // Client local UUID
        public decimal Amount { get; set; }     // Expense amount
        public string Note { get; set; } = string.Empty;        // Description / note
        public DateTime ExpenseDate { get; set; } // When the expense occurred
        public string? ImageBase64 { get; set; }  // Optional receipt image
    }
}
