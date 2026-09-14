using Microsoft.AspNetCore.Identity;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;

namespace MyApi.Services;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(user: null!, password);

    public bool Verify(string hash, string password)
    {
        PasswordVerificationResult result;

        try
        {
            result = _hasher.VerifyHashedPassword(user: null!, hash, password);
        }
        catch (FormatException)
        {
            return false;
        }

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
