using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        // Generate a 128-bit salt using a secure PRNG
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derive a 256-bit subkey (PBKDF2)
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));

        // Store salt + hash together
        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (storedHash.StartsWith("AQAAAA"))
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword(null, storedHash, password);
            return result != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
        }

        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));

        return hash == parts[1];
    }
}
