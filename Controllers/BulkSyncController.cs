using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseTracker.API.Models;
using Dapper;
using System.Text.Json;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/sync")]
    public class BulkSyncController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly UserRepository _userRepo;

        public BulkSyncController(DbConnectionFactory dbFactory, UserRepository userRepo)
        {
            _dbFactory = dbFactory;
            _userRepo = userRepo;
        }

        [HttpPost]
        public async Task<IActionResult> Sync([FromBody] SyncPayload payload)
        {
            using var db = _dbFactory.CreateConnection();
            db.Open();
            using var transaction = db.BeginTransaction();
            
            var result = new SyncResult();

            try
            {
                foreach (var action in payload.Actions)
                {
                    bool success = await ProcessAction(action, db, transaction, _userRepo);
                    if (success)
                    {
                        result.Successful.Add(action.Id);
                    }
                    else
                    {
                        result.Failed.Add(action.Id);
                    }
                }
                
                transaction.Commit();
                return Ok(result);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "Sync failed due to an internal error.", details = ex.Message });
            }
        }

        private async Task<bool> ProcessAction(SyncAction action, System.Data.IDbConnection db, System.Data.IDbTransaction transaction, UserRepository userRepo)
        {
            string tableName = action.Collection switch
            {
                "expenses" => "Expenses",
                "categories" => "Categories",
                "finance/profile" => "FinanceProfiles",
                "finance/extra-income" => "ExtraIncomes",
                _ => null
            };

            if (tableName == null) return false;

            string idField = tableName == "FinanceProfiles" ? "UserId" : "Id";
            string idValue = GetIdFromPayload(action.Payload, idField);

            if (tableName == "Expenses" && string.IsNullOrEmpty(idValue))
            {
                idValue = GetIdFromPayload(action.Payload, "ClientExpenseId") ?? GetIdFromPayload(action.Payload, "ClientId");
                if (!string.IsNullOrEmpty(idValue))
                {
                    idField = "ClientExpenseId";
                }
            }

            if (string.IsNullOrEmpty(idValue) && action.Action != "CREATE") return false;

            if (!string.IsNullOrEmpty(idValue))
            {
                // Check existing timestamp
                long? existingTimestamp = await db.QueryFirstOrDefaultAsync<long?>(
                    $"SELECT Timestamp FROM {tableName} WHERE {idField} = @Id", 
                    new { Id = idValue }, 
                    transaction);

                if (existingTimestamp.HasValue && existingTimestamp.Value > action.Timestamp)
                {
                    return true; 
                }
            }

            if (action.Action == "DELETE")
            {
                string sql = $"UPDATE {tableName} SET IsDeleted = 1, Timestamp = @Timestamp WHERE {idField} = @Id";
                await db.ExecuteAsync(sql, new { Timestamp = action.Timestamp, Id = idValue }, transaction);
                return true;
            }
            else if (action.Action == "CREATE" || action.Action == "UPDATE")
            {
                if (tableName == "Expenses")
                {
                    var entity = JsonSerializer.Deserialize<Expense>(action.Payload.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (entity != null)
                    {
                        // Check if title / description was passed instead of note
                        if (string.IsNullOrWhiteSpace(entity.Note))
                        {
                            if (action.Payload.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                                entity.Note = titleProp.GetString();
                            else if (action.Payload.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                                entity.Note = descProp.GetString();
                        }

                        // Check if date was passed instead of expenseDate
                        if (entity.ExpenseDate == default)
                        {
                            if (action.Payload.TryGetProperty("date", out var dateProp) &&
                                dateProp.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(dateProp.GetString(), out var parsedDate))
                            {
                                entity.ExpenseDate = parsedDate;
                            }
                            else
                            {
                                entity.ExpenseDate = DateTime.UtcNow;
                            }
                        }

                        // Resolve firebaseUid → integer userId when userId is not set
                        if (entity.UserId <= 0)
                        {
                            string? firebaseUid = entity.FirebaseUid;
                            if (string.IsNullOrWhiteSpace(firebaseUid))
                            {
                                if (action.Payload.TryGetProperty("firebaseUid", out var fuidProp) && fuidProp.ValueKind == JsonValueKind.String)
                                {
                                    firebaseUid = fuidProp.GetString();
                                }
                                else if (action.Payload.TryGetProperty("FirebaseUid", out fuidProp) && fuidProp.ValueKind == JsonValueKind.String)
                                {
                                    firebaseUid = fuidProp.GetString();
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(firebaseUid))
                            {
                                var user = await userRepo.GetByFirebaseUidAsync(firebaseUid);
                                if (user != null)
                                {
                                    entity.UserId = user.Id;
                                }
                                else
                                {
                                    // Automatically create user in SQL database if not exists yet
                                    entity.UserId = await userRepo.GetOrCreateUser(firebaseUid, $"{firebaseUid}@expense-tracker.com");
                                }
                            }

                            if (entity.UserId <= 0) entity.UserId = 1; // Fallback default (User 1 exists in DB)
                        }

                        // Ensure ClientExpenseId is populated
                        if (string.IsNullOrWhiteSpace(entity.ClientExpenseId))
                        {
                            entity.ClientExpenseId = entity.ClientId ?? action.Id;
                        }

                        long ts = action.Timestamp > 0 
                            ? action.Timestamp 
                            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        string sql = @"
                            -- Ensure CategoryId exists, otherwise fallback to 1 (Food)
                            IF (@CategoryId <= 0 OR NOT EXISTS (SELECT 1 FROM Categories WHERE Id = @CategoryId))
                                SET @CategoryId = 1;

                            IF EXISTS (SELECT 1 FROM Expenses WHERE (Id = @Id AND @Id > 0) OR (ClientExpenseId = @ClientExpenseId AND @ClientExpenseId IS NOT NULL AND @ClientExpenseId <> ''))
                                UPDATE Expenses 
                                SET CategoryId=@CategoryId, Amount=@Amount, Note=@Note, ExpenseDate=@ExpenseDate, ImageBase64=@ImageBase64,
                                    UpdatedAt=GETUTCDATE(), IsDeleted=0,
                                    [Timestamp]=@Timestamp
                                WHERE (Id = @Id AND @Id > 0) OR (ClientExpenseId = @ClientExpenseId AND @ClientExpenseId IS NOT NULL AND @ClientExpenseId <> '')
                            ELSE
                                INSERT INTO Expenses (UserId, CategoryId, Amount, Note, ExpenseDate, ImageBase64, ClientId, ClientExpenseId, CreatedAt, UpdatedAt, IsDeleted, [Timestamp]) 
                                VALUES (@UserId, @CategoryId, @Amount, @Note, @ExpenseDate, @ImageBase64, @ClientId, @ClientExpenseId, GETUTCDATE(), GETUTCDATE(), 0,
                                        @Timestamp)";

                        await db.ExecuteAsync(sql, new
                        {
                            entity.Id,
                            entity.UserId,
                            entity.CategoryId,
                            entity.Amount,
                            Note = entity.Note ?? "Expense",
                            entity.ExpenseDate,
                            entity.ImageBase64,
                            entity.ClientId,
                            entity.ClientExpenseId,
                            Timestamp = ts
                        }, transaction);
                    }
                }
                else if (tableName == "Categories")
                {
                    var entity = JsonSerializer.Deserialize<Category>(action.Payload.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (entity != null)
                    {
                        string trimmedName = entity.Name?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(trimmedName))
                        {
                            return true;
                        }

                        entity.Timestamp = action.Timestamp > 0 ? action.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        if ((!entity.UserId.HasValue || entity.UserId.Value <= 0) && !string.IsNullOrWhiteSpace(entity.FirebaseUid))
                        {
                            var user = await userRepo.GetByFirebaseUidAsync(entity.FirebaseUid);
                            if (user != null)
                            {
                                entity.UserId = user.Id;
                            }
                            else
                            {
                                entity.UserId = await userRepo.GetOrCreateUser(entity.FirebaseUid, $"{entity.FirebaseUid}@expense-tracker.com");
                            }
                        }

                        // Check if category already exists by Id or by Name
                        var existing = await db.QueryFirstOrDefaultAsync<Category>(
                            @"SELECT TOP 1 * FROM Categories 
                              WHERE (Id = @Id AND @Id > 0) 
                                 OR (LOWER(LTRIM(RTRIM(Name))) = LOWER(@Name) AND (UserId IS NULL OR UserId = @UserId))",
                            new { entity.Id, Name = trimmedName, entity.UserId },
                            transaction
                        );

                        if (existing != null)
                        {
                            // System categories (UserId IS NULL or Id <= 8) must NEVER be updated or overwritten!
                            if (existing.UserId == null || existing.Id <= 8)
                            {
                                // Strictly do nothing to default/system categories
                                return true;
                            }

                            // For user custom category, only update if action is explicitly UPDATE
                            if (action.Action == "UPDATE")
                            {
                                string sqlUpdate = @"
                                    UPDATE Categories 
                                    SET Name = @Name, Icon = @Icon, Color = @Color, Timestamp = @Timestamp, LastUpdated = GETUTCDATE() 
                                    WHERE Id = @Id AND UserId = @UserId";
                                await db.ExecuteAsync(sqlUpdate, new
                                {
                                    Name = trimmedName,
                                    entity.Icon,
                                    entity.Color,
                                    entity.Timestamp,
                                    existing.Id,
                                    entity.UserId
                                }, transaction);
                            }
                        }
                        else
                        {
                            // Category does not exist: only insert if this is creating a new custom category
                            string sqlInsert = @"
                                INSERT INTO Categories (Name, Icon, Color, Timestamp, IsDeleted, UserId, CreatedAt, LastUpdated) 
                                VALUES (@Name, @Icon, @Color, @Timestamp, 0, @UserId, GETUTCDATE(), GETUTCDATE())";
                            await db.ExecuteAsync(sqlInsert, new
                            {
                                Name = trimmedName,
                                Icon = entity.Icon ?? "category",
                                Color = entity.Color ?? "#2196F3",
                                entity.Timestamp,
                                entity.UserId
                            }, transaction);
                        }
                    }
                }
                else if (tableName == "FinanceProfiles")
                {
                    var entity = JsonSerializer.Deserialize<FinanceProfile>(action.Payload.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (entity != null)
                    {
                        entity.Timestamp = action.Timestamp;
                        string sql = @"
                            IF EXISTS (SELECT * FROM FinanceProfiles WHERE UserId = @UserId)
                                UPDATE FinanceProfiles SET MonthlyIncome=@MonthlyIncome, SavingsPercentage=@SavingsPercentage, FixedExpenses=@FixedExpenses, Timestamp=@Timestamp, IsDeleted=0 WHERE UserId=@UserId
                            ELSE
                                INSERT INTO FinanceProfiles (UserId, MonthlyIncome, SavingsPercentage, FixedExpenses, Timestamp, IsDeleted) VALUES (@UserId, @MonthlyIncome, @SavingsPercentage, @FixedExpenses, @Timestamp, 0)";
                        await db.ExecuteAsync(sql, entity, transaction);
                    }
                }
                else if (tableName == "ExtraIncomes")
                {
                    var entity = JsonSerializer.Deserialize<ExtraIncome>(action.Payload.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (entity != null)
                    {
                        entity.Timestamp = action.Timestamp;
                        string sql = @"
                            IF EXISTS (SELECT * FROM ExtraIncomes WHERE Id = @Id)
                                UPDATE ExtraIncomes SET Source=@Source, Amount=@Amount, Date=@Date, UserId=@UserId, Timestamp=@Timestamp, IsDeleted=0 WHERE Id=@Id
                            ELSE
                                INSERT INTO ExtraIncomes (Id, Source, Amount, Date, UserId, Timestamp, IsDeleted) VALUES (@Id, @Source, @Amount, @Date, @UserId, @Timestamp, 0)";
                        await db.ExecuteAsync(sql, entity, transaction);
                    }
                }
                return true;
            }

            return false;
        }

        private string GetIdFromPayload(JsonElement payload, string idField)
        {
            if (payload.ValueKind == JsonValueKind.Object)
            {
                // Try camelCase first, then PascalCase
                string camelId = idField.Substring(0, 1).ToLower() + idField.Substring(1);
                if (payload.TryGetProperty(camelId, out var idProp))
                {
                    if (idProp.ValueKind == JsonValueKind.Number) return idProp.GetInt64().ToString();
                    return idProp.GetString();
                }
                if (payload.TryGetProperty(idField, out idProp))
                {
                    if (idProp.ValueKind == JsonValueKind.Number) return idProp.GetInt64().ToString();
                    return idProp.GetString();
                }
            }
            return null;
        }
    }
}
