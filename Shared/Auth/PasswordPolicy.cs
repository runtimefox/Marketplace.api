using MyApi.Models.Entities;

namespace MyApi.Shared.Auth;

public static class PasswordPolicy
{
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < UserAccount.PasswordMinLength)
        {
            return $"Password must be at least {UserAccount.PasswordMinLength} characters long.";
        }

        if (password.Length > UserAccount.RawPasswordMaxLength)
        {
            return $"Password must not exceed {UserAccount.RawPasswordMaxLength} characters.";
        }

        return null;
    }
}
