using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseTracker.API.DTOs;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly ExpenseRepository _expenseRepo;
        private readonly UserRepository _userRepo;

        public ExpensesController(ExpenseRepository expenseRepo, UserRepository userRepo)
        {
            _expenseRepo = expenseRepo;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] int? userId, [FromQuery] string? firebaseUid)
        {
            if (userId.HasValue && userId.Value > 0)
            {
                var expenses = await _expenseRepo.GetExpensesByUserAsync(userId.Value);
                return Ok(expenses);
            }

            if (!string.IsNullOrWhiteSpace(firebaseUid))
            {
                var user = await _userRepo.GetByFirebaseUidAsync(firebaseUid);
                if (user != null)
                {
                    var expenses = await _expenseRepo.GetExpensesByUserAsync(user.Id);
                    return Ok(expenses);
                }
                // firebaseUid provided but user not found in DB → return empty (never leak other users' data)
                return Ok(new List<object>());
            }

            // No identifier provided → return empty list for safety (never expose all users' data)
            return Ok(new List<object>());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseById(int id)
        {
            if (id <= 0) return BadRequest("Valid id is required.");
            var expense = await _expenseRepo.GetExpenseByIdAsync(id);
            if (expense == null) return NotFound();
            return Ok(expense);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] ExpenseDto expense, [FromQuery] string? firebaseUid)
        {
            // Resolve userId from FirebaseUid — check body field first, then query param
            if (expense.UserId <= 0)
            {
                var uid = !string.IsNullOrWhiteSpace(expense.FirebaseUid)
                    ? expense.FirebaseUid
                    : firebaseUid;

                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var user = await _userRepo.GetByFirebaseUidAsync(uid);
                    if (user != null)
                    {
                        expense.UserId = user.Id;
                    }
                    else
                    {
                        expense.UserId = await _userRepo.GetOrCreateUser(uid, $"{uid}@expense-tracker.com");
                    }
                }
            }

            if (expense.UserId <= 0)
            {
                expense.UserId = 1; // Fallback default user
            }

            if (expense.CategoryId <= 0)
            {
                expense.CategoryId = 1; // Fallback default category (Food)
            }

            if (expense.ExpenseDate == default)
            {
                expense.ExpenseDate = System.DateTime.UtcNow;
            }

            var newId = await _expenseRepo.AddExpenseAsync(expense);
            expense.Id = newId;
            return StatusCode(201, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseDto expense)
        {
            if (id <= 0) return BadRequest("Valid id is required.");
            expense.Id = id;
            var success = await _expenseRepo.UpdateExpenseAsync(expense);
            if (!success) return NotFound();
            return Ok(expense);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            if (id <= 0) return BadRequest("Valid id is required.");
            var success = await _expenseRepo.DeleteExpenseAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Expense deleted successfully." });
        }
    }
}
