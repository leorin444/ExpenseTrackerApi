using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    public class BudgetController : ControllerBase
    {
        private readonly BudgetRepository _repo;
        private readonly UserRepository _userRepo;

        public BudgetController(BudgetRepository repo, UserRepository userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

        [HttpGet("api/sync/budgets")]
        [HttpGet("api/budgets")]
        public async Task<IActionResult> GetBudget()
        {
            var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();
            if (string.IsNullOrEmpty(firebaseUid))
                return Unauthorized("Missing or invalid user authentication.");

            var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

            var budget = await _repo.GetUserBudget(userId);
            return Ok(budget);
        }
    }
}