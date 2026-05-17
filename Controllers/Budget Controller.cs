using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sync")]
public class BudgetController : ControllerBase
{
    private readonly BudgetRepository _repo;
    private readonly UserRepository _userRepo;

    public BudgetController(BudgetRepository repo, UserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudget()
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();
        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var budget = await _repo.GetUserBudget(userId);
        return Ok(budget);
    }
}