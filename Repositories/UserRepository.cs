using Dapper;

using ExpenseTracker.API.Models;

public class UserRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public UserRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> GetOrCreateUser(string firebaseUid, string email)
    {
        using var db = _dbFactory.CreateConnection();

        var userId = await db.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM Users WHERE FirebaseUid=@Uid",
            new { Uid = firebaseUid }
        );

        if (userId.HasValue)
        {
            // update last login
            await db.ExecuteAsync(
                "UPDATE Users SET LastLogin=GETUTCDATE() WHERE Id=@Id",
                new { Id = userId.Value }
            );
            return userId.Value;
        }

        // insert new user
        var newId = await db.ExecuteScalarAsync<int>(@"
            INSERT INTO Users (FirebaseUid, Email, LastLogin)
            VALUES (@Uid, @Email, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);
        ", new { Uid = firebaseUid, Email = email });

        return newId;
    }


    public async Task<User> GetUserByEmail(string email)
    {
        using var db = _dbFactory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<User>(
            "SELECT Id, Email, PasswordHash, FirebaseUid, LastLogin FROM Users WHERE Email=@Email",
            new { Email = email }
        );
    }

    public async Task<int> CreateUser(string email, string passwordHash, string firebaseUid)
    {
        using var db = _dbFactory.CreateConnection();
        var userId = await db.ExecuteScalarAsync<int>(@"
        INSERT INTO Users (Email, PasswordHash, FirebaseUid, LastLogin)
        VALUES (@Email, @PasswordHash, @FirebaseUid, GETUTCDATE());
        SELECT CAST(SCOPE_IDENTITY() as int);
    ", new { Email = email, PasswordHash = passwordHash, FirebaseUid = firebaseUid });

        return userId;
    }



    public async Task<User?> GetByFirebaseUidAsync(string firebaseUid)
    {
        using var db = _dbFactory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE FirebaseUid=@FirebaseUid", new { FirebaseUid = firebaseUid });
    }

}