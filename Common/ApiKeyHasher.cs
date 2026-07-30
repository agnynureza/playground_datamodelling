using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace TenantService.Api.Common;

public static class PasswordHelper
{
    private static readonly PasswordHasher<object> Hasher = new();
    
    // generate APIkey
    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public static string Hash(string password)
    {
        return Hasher.HashPassword(null!, password);
    }

    public static bool Verify(string password, string hashedPassword)
    {
        var result = Hasher.VerifyHashedPassword(
            null!,
            hashedPassword,
            password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
