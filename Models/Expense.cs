using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseTracker.API.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }

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

        public string? FirebaseUid { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public string? ImageBase64 { get; set; }
        public string? ClientId { get; set; }
        public string? ClientExpenseId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public long Timestamp { get; set; }
    }
}
