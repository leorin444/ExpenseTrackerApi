using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sync")]
public class CategoryController : ControllerBase
{
    private readonly CategoryRepository _repo;

    public CategoryController(CategoryRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _repo.GetAllCategories();
        return Ok(categories);
    }
}