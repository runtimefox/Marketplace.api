namespace MyApi.Models.Dtos.Auth;


public record AuthResponseDto
{
    public required DateTime ExpiresAt { get; init; }
}
