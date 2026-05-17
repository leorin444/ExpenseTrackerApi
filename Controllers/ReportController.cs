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
    // GET /api/reports/monthly?year=2026
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport(int year)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();

        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _repo.GetMonthlyReport(userId, year);

        return Ok(result);
    }

    // CATEGORY REPORT
    // GET /api/reports/category?month=6&year=2026
    [HttpGet("category")]
    public async Task<IActionResult> GetCategoryReport(int month, int year)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();

        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _repo.GetCategoryReport(userId, month, year);

        return Ok(result);
    }

    // DAILY REPORT
    // GET /api/reports/daily?month=6&year=2026
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport(int month, int year)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();

        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _repo.GetDailyReport(userId, month, year);

        return Ok(result);
    }

    // BUDGET REPORT
    // GET /api/reports/budget?month=6&year=2026
    [HttpGet("budget")]
    public async Task<IActionResult> GetBudgetReport(int month, int year)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();

        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _repo.GetBudgetReport(userId, month, year);

        return Ok(result);
    }

    // YEAR SUMMARY
    // GET /api/reports/year?year=2026
    [HttpGet("year")]
    public async Task<IActionResult> GetYearReport(int year)
    {
        var firebaseUid = HttpContext.Items["FirebaseUid"]?.ToString();

        if (firebaseUid == null)
            return Unauthorized();

        var userId = await _userRepo.GetOrCreateUser(firebaseUid, "user@example.com");

        var result = await _repo.GetYearReport(userId, year);

        return Ok(result);
    }
}