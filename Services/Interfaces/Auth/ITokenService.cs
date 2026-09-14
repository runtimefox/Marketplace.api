using MyApi.Models.Dtos.Users;

namespace MyApi.Services.Interfaces.Auth;

public interface ITokenService
{
    AccessToken GenerateToken(UserDto user);
    RefreshTokenValue GenerateRefreshToken();
    string HashRefreshToken(string token);
}

public record AccessToken(string Value, DateTime ExpiresAt);

public record RefreshTokenValue(string Value, string Hash, DateTime ExpiresAt);
