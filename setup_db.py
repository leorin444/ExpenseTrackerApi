import os
import json

base_dir = r"a:\API\ExpenseTracker.API"

# 1. Update appsettings.json
appsettings_path = os.path.join(base_dir, "appsettings.json")
with open(appsettings_path, 'r', encoding='utf-8') as f:
    settings = json.load(f)

settings['ConnectionStrings'] = {
    'DefaultConnection': 'Server=192.168.0.106;Database=expense_tracker;User Id=sa;Password=Le0rin44;TrustServerCertificate=True;'
}

with open(appsettings_path, 'w', encoding='utf-8') as f:
    json.dump(settings, f, indent=2)

# 2. Add DbInitializer to Database folder
db_init_code = """using Dapper;
using System.Data;

public class DbInitializer
{
    private readonly DbConnectionFactory _connectionFactory;

    public DbInitializer(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
            CREATE TABLE Expenses (
                Id NVARCHAR(50) PRIMARY KEY,
                UserId NVARCHAR(100),
                Title NVARCHAR(255),
                Amount FLOAT,
                Category NVARCHAR(100),
                Date NVARCHAR(50),
                Timestamp BIGINT
            );

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
            CREATE TABLE Categories (
                Id NVARCHAR(50) PRIMARY KEY,
                Name NVARCHAR(100)
            );

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinanceProfiles')
            CREATE TABLE FinanceProfiles (
                UserId NVARCHAR(100) PRIMARY KEY,
                MonthlyIncome FLOAT,
                SavingsPercentage FLOAT,
                FixedExpenses FLOAT
            );

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExtraIncomes')
            CREATE TABLE ExtraIncomes (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Source NVARCHAR(255),
                Amount FLOAT,
                Date NVARCHAR(50),
                UserId NVARCHAR(100)
            );
        ";

        connection.Execute(sql);
    }
}
"""

with open(os.path.join(base_dir, "Database", "DbInitializer.cs"), "w", encoding="utf-8") as f:
    f.write(db_init_code)

# 3. Rewrite Controllers with Dapper
controllers_dir = os.path.join(base_dir, "Controllers")

expenses_controller = """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;

        public ExpensesController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var expenses = await db.QueryAsync<Expense>("SELECT * FROM Expenses WHERE UserId = @UserId", new { UserId = userId });
            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] Expense expense)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "INSERT INTO Expenses (Id, UserId, Title, Amount, Category, Date, Timestamp) VALUES (@Id, @UserId, @Title, @Amount, @Category, @Date, @Timestamp)";
            await db.ExecuteAsync(sql, expense);
            return StatusCode(201, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(string id, [FromBody] Expense expense)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "UPDATE Expenses SET Title=@Title, Amount=@Amount, Category=@Category, Date=@Date, Timestamp=@Timestamp WHERE Id=@Id";
            expense.Id = id;
            var rows = await db.ExecuteAsync(sql, expense);
            if (rows == 0) return NotFound();
            return Ok(expense);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(string id)
        {
            using var db = _dbFactory.CreateConnection();
            var rows = await db.ExecuteAsync("DELETE FROM Expenses WHERE Id=@Id", new { Id = id });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
"""

categories_controller = """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;

        public CategoriesController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            using var db = _dbFactory.CreateConnection();
            var categories = await db.QueryAsync<Category>("SELECT * FROM Categories");
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "INSERT INTO Categories (Id, Name) VALUES (@Id, @Name)";
            await db.ExecuteAsync(sql, category);
            return StatusCode(201, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromBody] Category category)
        {
            using var db = _dbFactory.CreateConnection();
            category.Id = id;
            var rows = await db.ExecuteAsync("UPDATE Categories SET Name=@Name WHERE Id=@Id", category);
            if (rows == 0) return NotFound();
            return Ok(category);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            using var db = _dbFactory.CreateConnection();
            var rows = await db.ExecuteAsync("DELETE FROM Categories WHERE Id=@Id", new { Id = id });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
"""

finance_profile_controller = """using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("finance/profile")]
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
            var profile = await db.QueryFirstOrDefaultAsync<FinanceProfile>("SELECT * FROM FinanceProfiles WHERE UserId = @UserId", new { UserId = userId });
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] FinanceProfile profile)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "INSERT INTO FinanceProfiles (UserId, MonthlyIncome, SavingsPercentage, FixedExpenses) VALUES (@UserId, @MonthlyIncome, @SavingsPercentage, @FixedExpenses)";
            try {
                await db.ExecuteAsync(sql, profile);
            } catch {
                return BadRequest("Profile already exists.");
            }
            return StatusCode(201, profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] FinanceProfile profile)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "UPDATE FinanceProfiles SET MonthlyIncome=@MonthlyIncome, SavingsPercentage=@SavingsPercentage, FixedExpenses=@FixedExpenses WHERE UserId=@UserId";
            var rows = await db.ExecuteAsync(sql, profile);
            if (rows == 0) return NotFound();
            return Ok(profile);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProfile([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var rows = await db.ExecuteAsync("DELETE FROM FinanceProfiles WHERE UserId=@UserId", new { UserId = userId });
            if (rows == 0) return NotFound();
            return Ok();
        }
    }
}
"""

extra_income_controller = """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;
using Dapper;
using System.Threading.Tasks;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("finance/extra-income")]
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
            var incomes = await db.QueryAsync<ExtraIncome>("SELECT * FROM ExtraIncomes WHERE UserId = @UserId", new { UserId = userId });
            return Ok(incomes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExtraIncome([FromBody] ExtraIncome income)
        {
            using var db = _dbFactory.CreateConnection();
            var sql = "INSERT INTO ExtraIncomes (Source, Amount, Date, UserId) VALUES (@Source, @Amount, @Date, @UserId)";
            await db.ExecuteAsync(sql, income);
            return StatusCode(201, income);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteExtraIncomes([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            using var db = _dbFactory.CreateConnection();
            var rows = await db.ExecuteAsync("DELETE FROM ExtraIncomes WHERE UserId=@UserId", new { UserId = userId });
            return Ok();
        }
    }
}
"""

controllers = {
    "ExpensesController.cs": expenses_controller,
    "CategoriesController.cs": categories_controller,
    "FinanceProfileController.cs": finance_profile_controller,
    "ExtraIncomeController.cs": extra_income_controller,
}

for filename, content in controllers.items():
    with open(os.path.join(controllers_dir, filename), "w", encoding="utf-8") as f:
        f.write(content)

# 4. Update Program.cs
program_path = os.path.join(base_dir, "Program.cs")
program_content = """using Microsoft.OpenApi.Models;
using ExpenseTracker.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<DbInitializer>();

var app = builder.Build();

// Initialize the database tables if they don't exist
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    try {
        initializer.Initialize();
    } catch (System.Exception ex) {
        System.Console.WriteLine("Could not initialize DB: " + ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API v1");
    c.RoutePrefix = "swagger"; 
});

app.UseHttpsRedirection();

app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
"""

with open(program_path, "w", encoding="utf-8") as f:
    f.write(program_content)

print("DB Integration setup complete")
