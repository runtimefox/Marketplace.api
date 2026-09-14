namespace MyApi.Models.Dtos.Auth;

public record AuthResultDto
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshExpiresAt { get; init; }
}
