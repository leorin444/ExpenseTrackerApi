using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/finance/extra-income")]
    public class ExtraIncomeController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;

        public ExtraIncomeController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetExtraIncomes([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var incomes = await db.QueryAsync<ExtraIncome>("SELECT * FROM ExtraIncomes WHERE UserId = @UserId AND IsDeleted = 0", new { UserId = userId });
            return Ok(incomes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExtraIncome([FromBody] ExtraIncome income)
        {
            if (string.IsNullOrEmpty(income.Id))
            {
                income.Id = Guid.NewGuid().ToString();
            }
            if (income.Timestamp == 0)
            {
                income.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            using var db = _dbFactory.CreateConnection();
            string sql = @"
                IF EXISTS (SELECT * FROM ExtraIncomes WHERE Id = @Id)
                BEGIN
                    DECLARE @ExistingTimestamp BIGINT;
                    SELECT @ExistingTimestamp = Timestamp FROM ExtraIncomes WHERE Id = @Id;
                    IF (@ExistingTimestamp <= @Timestamp)
                        UPDATE ExtraIncomes SET Source=@Source, Amount=@Amount, Date=@Date, UserId=@UserId, Timestamp=@Timestamp, IsDeleted=0 WHERE Id=@Id
                END
                ELSE
                    INSERT INTO ExtraIncomes (Id, Source, Amount, Date, UserId, Timestamp, IsDeleted) VALUES (@Id, @Source, @Amount, @Date, @UserId, @Timestamp, 0)";
            await db.ExecuteAsync(sql, income);
            return StatusCode(201, income);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExtraIncome(string id, [FromBody] ExtraIncome income)
        {
            using var db = _dbFactory.CreateConnection();
            income.Id = id;
            if (income.Timestamp == 0)
            {
                income.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            var existingTimestamp = await db.QueryFirstOrDefaultAsync<long?>("SELECT Timestamp FROM ExtraIncomes WHERE Id = @Id", new { Id = id });
            
            if (existingTimestamp.HasValue && existingTimestamp.Value > income.Timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var sql = "UPDATE ExtraIncomes SET Source=@Source, Amount=@Amount, Date=@Date, UserId=@UserId, Timestamp=@Timestamp, IsDeleted=0 WHERE Id=@Id";
            var rows = await db.ExecuteAsync(sql, income);
            if (rows == 0) return NotFound();
            return Ok(income);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExtraIncome(string id, [FromQuery] long timestamp = 0)
        {
            using var db = _dbFactory.CreateConnection();
            var existingTimestamp = await db.QueryFirstOrDefaultAsync<long?>("SELECT Timestamp FROM ExtraIncomes WHERE Id = @Id", new { Id = id });
            
            if (timestamp > 0 && existingTimestamp.HasValue && existingTimestamp.Value > timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var t = timestamp <= 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp;
            var rows = await db.ExecuteAsync("UPDATE ExtraIncomes SET IsDeleted=1, Timestamp=@Timestamp WHERE Id=@Id", new { Id = id, Timestamp = t });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
