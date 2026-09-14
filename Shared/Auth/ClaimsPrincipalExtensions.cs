using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MyApi.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

    public static Guid GetRequiredUserId(this ClaimsPrincipal principal) =>
        principal.TryGetUserId(out var userId)
            ? userId
            : throw new UnauthorizedAccessException("The token does not contain a user identifier.");
}
