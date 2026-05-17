using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.API.Repositories;
using ExpenseTracker.API.DTOs;
using Microsoft.AspNetCore.Authorization;


namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly ExpenseRepository _repository;

        public ExpensesController(ExpenseRepository repository)
        {
            _repository = repository;
        }

        // POST /api/expenses
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] ExpenseDto expense)
        {
            var id = await _repository.AddExpenseAsync(expense);
            return Ok(new { message = "Expense added successfully", expenseId = id });
        }

        // GET /api/expenses/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseById(int id)
        {
            var expense = await _repository.GetExpenseByIdAsync(id);
            if (expense == null) return NotFound();
            return Ok(expense);
        }

        // PUT /api/expenses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseDto expense)
        {
            if (id != expense.Id) return BadRequest(new { message = "Expense ID mismatch" });

            var updated = await _repository.UpdateExpenseAsync(expense);
            if (!updated) return NotFound(new { message = "Expense not found" });

            return Ok(new { message = "Expense updated successfully" });
        }

        // DELETE /api/expenses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var deleted = await _repository.DeleteExpenseAsync(id);
            if (!deleted) return NotFound(new { message = "Expense not found" });

            return Ok(new { message = "Expense deleted successfully" });
        }


        // GET /api/expenses → List all expenses for logged-in user
        [HttpGet]
        public async Task<IActionResult> GetExpensesForUser()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Unauthorized" });

            int userId = int.Parse(userIdClaim);
            var expenses = await _repository.GetExpensesByUserAsync(userId);

            return Ok(new { message = "Expenses retrieved successfully", data = expenses });
        }



    }
}
