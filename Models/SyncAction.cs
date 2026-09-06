using System.Collections.Generic;
using System.Text.Json;

namespace ExpenseTracker.API.Models
{
    public class SyncPayload
    {
        public List<SyncAction> Actions { get; set; } = new();
    }

    public class SyncAction
    {
        public string Id { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public JsonElement Payload { get; set; }
        public long Timestamp { get; set; }
    }
    
    public class SyncResult
    {
        public List<string> Successful { get; set; } = new();
        public List<string> Failed { get; set; } = new();
    }
}
