using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly ReportRepository _repo;
    private readonly UserRepository _userRepo;

    public ReportController(ReportRepository repo, UserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }




    // MONTHLY REPORT
    // GET /api/reports/monthly?year=2026&userId=1
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int? userId = null)
    {
        var uid = await ResolveUserId(userId);
        if (!uid.HasValue) return Unauthorized("Authentication required.");

        var result = await _repo.GetMonthlyReport(uid.Value, year);
        return Ok(result);
    }

    // CATEGORY REPORT
    // GET /api/reports/category?month=6&year=2026&userId=1
    [HttpGet("category")]
    public async Task<IActionResult> GetCategoryReport([FromQuery] int month, [FromQuery] int year, [FromQuery] int? userId = null)
    {
        var uid = await ResolveUserId(userId);
        if (!uid.HasValue) return Unauthorized("Authentication required.");

        var result = await _repo.GetCategoryReport(uid.Value, month, year);
        return Ok(result);
    }

    // DAILY REPORT
    // GET /api/reports/daily?month=6&year=2026&userId=1
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport([FromQuery] int month, [FromQuery] int year, [FromQuery] int? userId = null)
    {
        var uid = await ResolveUserId(userId);
        if (!uid.HasValue) return Unauthorized("Authentication required.");

        var result = await _repo.GetDailyReport(uid.Value, month, year);
        return Ok(result);
    }

    // BUDGET REPORT
    // GET /api/reports/budget?month=6&year=2026&userId=1
    [HttpGet("budget")]
    public async Task<IActionResult> GetBudgetReport([FromQuery] int month, [FromQuery] int year, [FromQuery] int? userId = null)
    {
        var uid = await ResolveUserId(userId);
        if (!uid.HasValue) return Unauthorized("Authentication required.");

        var result = await _repo.GetBudgetReport(uid.Value, month, year);
        return Ok(result);
    }

    // YEAR SUMMARY
    // GET /api/reports/year?year=2026&userId=1
    [HttpGet("year")]
    public async Task<IActionResult> GetYearReport([FromQuery] int year, [FromQuery] int? userId = null)
    {
        var uid = await ResolveUserId(userId);
        if (!uid.HasValue) return Unauthorized("Authentication required.");

        var result = await _repo.GetYearReport(uid.Value, year);
        return Ok(result);
    }

    private async Task<int?> ResolveUserId(int? userId)
    {
        if (userId.HasValue && userId.Value > 0)
        {
            return userId.Value;
        }

        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();
        if (!string.IsNullOrEmpty(firebaseUid))
        {
            return await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");
        }

        return null;
    }
}