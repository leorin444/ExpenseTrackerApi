using Dapper;

public class CategoryRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public CategoryRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<CategoryDto>> GetAllCategories()
    {
        using var db = _dbFactory.CreateConnection();

        var categories = await db.QueryAsync<CategoryDto>(
            "SELECT Id, Name, Icon FROM Categories"
        );

        return categories.ToList();
    }
}