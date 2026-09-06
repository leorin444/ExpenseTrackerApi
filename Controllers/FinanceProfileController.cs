using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/finance/profile")]
    public class FinanceProfileController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;

        public FinanceProfileController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var profile = await db.QueryFirstOrDefaultAsync<FinanceProfile>("SELECT * FROM FinanceProfiles WHERE UserId = @UserId AND IsDeleted = 0", new { UserId = userId });
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] FinanceProfile profile)
        {
            if (profile.Timestamp == 0)
            {
                profile.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            using var db = _dbFactory.CreateConnection();
            string sql = @"
                IF EXISTS (SELECT * FROM FinanceProfiles WHERE UserId = @UserId)
                BEGIN
                    DECLARE @ExistingTimestamp BIGINT;
                    SELECT @ExistingTimestamp = Timestamp FROM FinanceProfiles WHERE UserId = @UserId;
                    IF (@ExistingTimestamp <= @Timestamp)
                        UPDATE FinanceProfiles SET MonthlyIncome=@MonthlyIncome, SavingsPercentage=@SavingsPercentage, FixedExpenses=@FixedExpenses, Timestamp=@Timestamp, IsDeleted=0 WHERE UserId=@UserId
                END
                ELSE
                    INSERT INTO FinanceProfiles (UserId, MonthlyIncome, SavingsPercentage, FixedExpenses, Timestamp, IsDeleted) VALUES (@UserId, @MonthlyIncome, @SavingsPercentage, @FixedExpenses, @Timestamp, 0)";
            await db.ExecuteAsync(sql, profile);
            return StatusCode(201, profile);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(string userId, [FromBody] FinanceProfile profile)
        {
            using var db = _dbFactory.CreateConnection();
            profile.UserId = userId;
            if (profile.Timestamp == 0)
            {
                profile.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            var existingTimestamp = await db.QueryFirstOrDefaultAsync<long?>("SELECT Timestamp FROM FinanceProfiles WHERE UserId = @UserId", new { UserId = userId });
            
            if (existingTimestamp.HasValue && existingTimestamp.Value > profile.Timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var sql = "UPDATE FinanceProfiles SET MonthlyIncome=@MonthlyIncome, SavingsPercentage=@SavingsPercentage, FixedExpenses=@FixedExpenses, Timestamp=@Timestamp, IsDeleted=0 WHERE UserId=@UserId";
            var rows = await db.ExecuteAsync(sql, profile);
            if (rows == 0) return NotFound();
            return Ok(profile);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteProfile(string userId, [FromQuery] long timestamp = 0)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var existingTimestamp = await db.QueryFirstOrDefaultAsync<long?>("SELECT Timestamp FROM FinanceProfiles WHERE UserId = @UserId", new { UserId = userId });
            
            if (timestamp > 0 && existingTimestamp.HasValue && existingTimestamp.Value > timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var t = timestamp <= 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp;
            var rows = await db.ExecuteAsync("UPDATE FinanceProfiles SET IsDeleted=1, Timestamp=@Timestamp WHERE UserId=@UserId", new { UserId = userId, Timestamp = t });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
