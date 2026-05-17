using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly UserRepository _userRepo;
    private readonly ExpenseRepository _expenseRepo;

    public SyncController(UserRepository userRepo, ExpenseRepository expenseRepo)
    {
        _userRepo = userRepo;
        _expenseRepo = expenseRepo;
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> SyncExpenses([FromBody] SyncExpenseRequestDto request)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();
        if (firebaseUid == null)
            return Unauthorized();

        // email optional, can come from Flutter if needed
        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _expenseRepo.SyncExpenses(userId, request.Expenses, request.LastSyncTime);

        return Ok(result);
    }
}