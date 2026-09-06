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
            "SELECT Id, Name, Icon, Color, Timestamp FROM Categories WHERE ISNULL(IsDeleted, 0) = 0 ORDER BY Name"
        );

        return categories.ToList();
    }
}