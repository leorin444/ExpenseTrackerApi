using Dapper;
namespace ExpenseTracker.API.Repositories
{
    public class AuthRepository
    {
        private readonly DbConnectionFactory _dbFactory;
        public AuthRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task SaveRefreshToken(int userId, string refreshToken)
        {
            using var db = _dbFactory.CreateConnection();
            await db.ExecuteAsync(@"
                INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, CreatedAt)
                VALUES (@UserId, @Token, DATEADD(DAY, 30, GETUTCDATE()), GETUTCDATE());
            ", new { UserId = userId, Token = refreshToken });
        }

        public async Task<bool> ValidateRefreshToken(int userId, string refreshToken)
        {
            using var db = _dbFactory.CreateConnection();
            var token = await db.QueryFirstOrDefaultAsync<string>(@"
                SELECT Token FROM RefreshTokens
                WHERE UserId = @UserId AND Token = @Token AND ExpiresAt > GETUTCDATE()
            ", new { UserId = userId, Token = refreshToken });
            return token != null;
        }

        public async Task<int?> GetUserIdByRefreshToken(string refreshToken)
        {
            using var db = _dbFactory.CreateConnection();
            return await db.QueryFirstOrDefaultAsync<int?>(@"
                SELECT UserId FROM RefreshTokens
                WHERE Token = @Token AND ExpiresAt > GETUTCDATE()
            ", new { Token = refreshToken });
        }

        public async Task SaveResetToken(string email, string resetToken)
        {
            using var db = _dbFactory.CreateConnection();
            await db.ExecuteAsync(@"
                INSERT INTO PasswordResetTokens (Email, Token, ExpiresAt, CreatedAt)
                VALUES (@Email, @Token, DATEADD(HOUR, 1, GETUTCDATE()), GETUTCDATE());
            ", new { Email = email, Token = resetToken });
        }

        public async Task<bool> ValidateResetToken(string email, string resetToken)
        {
            using var db = _dbFactory.CreateConnection();
            var token = await db.QueryFirstOrDefaultAsync<string>(@"
                SELECT Token FROM PasswordResetTokens
                WHERE Email = @Email AND Token = @Token AND ExpiresAt > GETUTCDATE()
            ", new { Email = email, Token = resetToken });
            return token != null;
        }

        public async Task<bool> UpdatePassword(string email, string newPasswordHash)
        {
            using var db = _dbFactory.CreateConnection();
            var rows = await db.ExecuteAsync(@"
                UPDATE Users SET PasswordHash = @Hash WHERE Email = @Email
            ", new { Email = email, Hash = newPasswordHash });
            return rows > 0;
        }
    }
}