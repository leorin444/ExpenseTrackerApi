using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly UserRepository _userRepo;

        public CategoriesController(DbConnectionFactory dbFactory, UserRepository userRepo)
        {
            _dbFactory = dbFactory;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] int? userId, [FromQuery] string? firebaseUid)
        {
            // Resolve firebaseUid → integer userId if not directly provided
            if (!userId.HasValue && !string.IsNullOrWhiteSpace(firebaseUid))
            {
                var user = await _userRepo.GetByFirebaseUidAsync(firebaseUid);
                if (user != null) userId = user.Id;
            }

            using var db = _dbFactory.CreateConnection();
            var categories = await db.QueryAsync<Category>(
                "SELECT Id, Name, Icon, Color, Timestamp, IsDeleted, UserId FROM Categories WHERE ISNULL(IsDeleted, 0) = 0 AND (@UserId IS NULL OR UserId IS NULL OR UserId = @UserId) ORDER BY Name",
                new { UserId = userId }
            );
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            using var db = _dbFactory.CreateConnection();
            if (category.Timestamp == 0)
            {
                category.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Resolve FirebaseUid if needed
            if ((!category.UserId.HasValue || category.UserId.Value <= 0) && !string.IsNullOrWhiteSpace(category.FirebaseUid))
            {
                var user = await _userRepo.GetByFirebaseUidAsync(category.FirebaseUid);
                if (user != null) category.UserId = user.Id;
            }

            string trimmedName = category.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return BadRequest("Category name cannot be empty.");
            }

            // Check if category already exists by Id or by Name
            var existing = await db.QueryFirstOrDefaultAsync<Category>(
                @"SELECT TOP 1 Id, Name, Icon, Color, Timestamp, IsDeleted, UserId 
                  FROM Categories 
                  WHERE (Id = @Id AND @Id > 0) 
                     OR (LOWER(LTRIM(RTRIM(Name))) = LOWER(@Name) AND (UserId IS NULL OR UserId = @UserId))",
                new { category.Id, Name = trimmedName, category.UserId }
            );

            if (existing != null)
            {
                // Never overwrite existing categories when creating!
                // Simply return the existing category details.
                return Ok(existing);
            }

            // If it doesn't exist, insert new custom category
            string sqlInsert = @"
                INSERT INTO Categories (Name, Icon, Color, Timestamp, IsDeleted, UserId, CreatedAt, LastUpdated) 
                VALUES (@Name, @Icon, @Color, @Timestamp, 0, @UserId, GETUTCDATE(), GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            category.Name = trimmedName;
            category.Icon = category.Icon ?? "category";
            category.Color = category.Color ?? "#2196F3";
            category.Id = await db.ExecuteScalarAsync<int>(sqlInsert, category);
            return StatusCode(201, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id <= 8)
            {
                return BadRequest("Default system categories cannot be modified.");
            }

            using var db = _dbFactory.CreateConnection();

            var existing = await db.QueryFirstOrDefaultAsync<Category>("SELECT Id, UserId, Timestamp FROM Categories WHERE Id = @Id", new { Id = id });
            if (existing == null) return NotFound();

            if (existing.UserId == null || existing.Id <= 8)
            {
                return BadRequest("Default system categories cannot be modified.");
            }

            category.Id = id;
            if (category.Timestamp == 0)
            {
                category.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            if (existing.Timestamp > category.Timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var rows = await db.ExecuteAsync(
                "UPDATE Categories SET Name=@Name, Icon=@Icon, Color=@Color, Timestamp=@Timestamp, IsDeleted=0, LastUpdated=GETUTCDATE() WHERE Id=@Id", 
                category
            );
            if (rows == 0) return NotFound();
            return Ok(category);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, [FromQuery] long timestamp = 0)
        {
            if (id <= 8)
            {
                return BadRequest("Default system categories cannot be deleted.");
            }

            using var db = _dbFactory.CreateConnection();
            var existing = await db.QueryFirstOrDefaultAsync<Category>("SELECT Id, UserId, Timestamp FROM Categories WHERE Id = @Id", new { Id = id });
            if (existing == null) return NotFound();

            if (existing.UserId == null || existing.Id <= 8)
            {
                return BadRequest("Default system categories cannot be deleted.");
            }
            
            if (timestamp > 0 && existing.Timestamp > timestamp)
                return Conflict("Incoming timestamp is older than existing record.");

            var t = timestamp <= 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp;
            var rows = await db.ExecuteAsync("UPDATE Categories SET IsDeleted=1, Timestamp=@Timestamp, LastUpdated=GETUTCDATE() WHERE Id=@Id", new { Id = id, Timestamp = t });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
