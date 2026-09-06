import os

base_dir = r"a:\API\ExpenseTracker.API"

models_dir = os.path.join(base_dir, "Models")
controllers_dir = os.path.join(base_dir, "Controllers")
middleware_dir = os.path.join(base_dir, "Middleware")

os.makedirs(models_dir, exist_ok=True)
os.makedirs(controllers_dir, exist_ok=True)
os.makedirs(middleware_dir, exist_ok=True)

# 1. Models
models_code = {
    "Expense.cs": """using System;

namespace ExpenseTracker.API.Models
{
    public class Expense
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }
}
""",
    "Category.cs": """using System;

namespace ExpenseTracker.API.Models
{
    public class Category
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
    }
}
""",
    "FinanceProfile.cs": """namespace ExpenseTracker.API.Models
{
    public class FinanceProfile
    {
        public string UserId { get; set; } = string.Empty;
        public double MonthlyIncome { get; set; }
        public double SavingsPercentage { get; set; }
        public double FixedExpenses { get; set; }
    }
}
""",
    "ExtraIncome.cs": """namespace ExpenseTracker.API.Models
{
    public class ExtraIncome
    {
        public string Source { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
"""
}

for filename, content in models_code.items():
    with open(os.path.join(models_dir, filename), "w", encoding="utf-8") as f:
        f.write(content)

# 2. Controllers
controllers_code = {
    "ExpensesController.cs": """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("expenses")]
    public class ExpensesController : ControllerBase
    {
        private static readonly List<Expense> _expenses = new List<Expense>();

        [HttpGet]
        public IActionResult GetExpenses([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            var userExpenses = _expenses.Where(e => e.UserId == userId).ToList();
            return Ok(userExpenses);
        }

        [HttpPost]
        public IActionResult CreateExpense([FromBody] Expense expense)
        {
            _expenses.Add(expense);
            return StatusCode(201, expense);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateExpense(string id, [FromBody] Expense expense)
        {
            var existing = _expenses.FirstOrDefault(e => e.Id == id);
            if (existing == null) return NotFound();

            existing.Title = expense.Title;
            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.Date = expense.Date;
            existing.Timestamp = expense.Timestamp;

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteExpense(string id)
        {
            var existing = _expenses.FirstOrDefault(e => e.Id == id);
            if (existing == null) return NotFound();

            _expenses.Remove(existing);
            return Ok();
        }
    }
}
""",
    "CategoriesController.cs": """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoriesController : ControllerBase
    {
        private static readonly List<Category> _categories = new List<Category>();

        [HttpGet]
        public IActionResult GetCategories()
        {
            return Ok(_categories);
        }

        [HttpPost]
        public IActionResult CreateCategory([FromBody] Category category)
        {
            _categories.Add(category);
            return StatusCode(201, category);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateCategory(string id, [FromBody] Category category)
        {
            var existing = _categories.FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound();

            existing.Name = category.Name;
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteCategory(string id)
        {
            var existing = _categories.FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound();

            _categories.Remove(existing);
            return Ok();
        }
    }
}
""",
    "FinanceProfileController.cs": """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("finance/profile")]
    public class FinanceProfileController : ControllerBase
    {
        private static readonly List<FinanceProfile> _profiles = new List<FinanceProfile>();

        [HttpGet]
        public IActionResult GetProfile([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            var profile = _profiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost]
        public IActionResult CreateProfile([FromBody] FinanceProfile profile)
        {
            if (_profiles.Any(p => p.UserId == profile.UserId)) return BadRequest("Profile already exists.");
            _profiles.Add(profile);
            return StatusCode(201, profile);
        }

        [HttpPut]
        public IActionResult UpdateProfile([FromBody] FinanceProfile profile)
        {
            var existing = _profiles.FirstOrDefault(p => p.UserId == profile.UserId);
            if (existing == null) return NotFound();

            existing.MonthlyIncome = profile.MonthlyIncome;
            existing.SavingsPercentage = profile.SavingsPercentage;
            existing.FixedExpenses = profile.FixedExpenses;

            return Ok(existing);
        }

        [HttpDelete]
        public IActionResult DeleteProfile([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            var existing = _profiles.FirstOrDefault(p => p.UserId == userId);
            if (existing == null) return NotFound();

            _profiles.Remove(existing);
            return Ok();
        }
    }
}
""",
    "ExtraIncomeController.cs": """using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("finance/extra-income")]
    public class ExtraIncomeController : ControllerBase
    {
        private static readonly List<ExtraIncome> _incomes = new List<ExtraIncome>();

        [HttpGet]
        public IActionResult GetExtraIncomes([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            var incomes = _incomes.Where(i => i.UserId == userId).ToList();
            return Ok(incomes);
        }

        [HttpPost]
        public IActionResult CreateExtraIncome([FromBody] ExtraIncome income)
        {
            _incomes.Add(income);
            return StatusCode(201, income);
        }

        [HttpDelete]
        public IActionResult DeleteExtraIncomes([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("userId is required.");
            _incomes.RemoveAll(i => i.UserId == userId);
            return Ok();
        }
    }
}
"""
}

for filename, content in controllers_code.items():
    with open(os.path.join(controllers_dir, filename), "w", encoding="utf-8") as f:
        f.write(content)

# 3. Middleware
middleware_code = {
    "FirebaseAuthMiddleware.cs": """using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System;
using Google.Apis.Auth.OAuth2;

namespace ExpenseTracker.API.Middleware
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public FirebaseAuthMiddleware(RequestDelegate next)
        {
            _next = next;
            if (FirebaseApp.DefaultInstance == null)
            {
                try
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger") || context.Request.Path.StartsWithSegments("/v1/swagger.json"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Missing Authorization header");
                return;
            }

            string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    context.Items["User"] = decodedToken;
                }
                else 
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        throw new Exception("Invalid token");
                    }
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid Firebase token");
                return;
            }

            await _next(context);
        }
    }
}
"""
}

for filename, content in middleware_code.items():
    with open(os.path.join(middleware_dir, filename), "w", encoding="utf-8") as f:
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

var app = builder.Build();

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

print("Setup complete")
