using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Auth;
using MyApi.Models.Dtos.Users;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Users;
using MyApi.Shared.Auth;

namespace MyApi.Controllers;

[ApiController]
[Route("api/auth")]

public class AuthController : ControllerBase
{
    private const string MissingUserId = "The token does not contain a user identifier.";
    private const string UserNotFound = "User not found.";

    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _userService = userService;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(CreateUserDto createUser)
    {
        return Respond(await _authService.RegisterAsync(createUser));
    }

    [HttpPost("register-seller")]
    public async Task<ActionResult<AuthResponseDto>> RegisterSeller(RegisterSellerDto registerSeller)
    {
        return Respond(await _authService.RegisterSellerAsync(registerSeller));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto login)
    {
        return Respond(await _authService.LoginAsync(login));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(MissingUserId);
        }

        var result = await _userService.GetUserByIdAsync(userId);

        return result.Result is NotFoundResult
            ? Unauthorized(UserNotFound)
            : result;
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto updateProfile)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(MissingUserId);
        }

        var result = await _userService.UpdateProfileAsync(userId, updateProfile);

        return result.Result is NotFoundResult
            ? Unauthorized(UserNotFound)
            : result;
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<ActionResult<AuthResponseDto>> ChangePassword(ChangePasswordDto changePassword)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(MissingUserId);
        }

        return Respond(await _authService.ChangePasswordAsync(userId, changePassword));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken)
            || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized("Refresh token was not provided.");
        }

        return Respond(await _authService.RefreshAsync(refreshToken));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke()
    {
        if (Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken)
            && !string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeAsync(refreshToken);
        }

        DeleteAuthCookies();

        return NoContent();
    }

    private ActionResult<AuthResponseDto> Respond(ActionResult<AuthResultDto> result)
    {
        if (result.Result is not null)
        {
            return result.Result;
        }

        var auth = result.Value!;
        SetAuthCookies(auth);

        return new AuthResponseDto { ExpiresAt = auth.ExpiresAt };
    }

    private void SetAuthCookies(AuthResultDto auth)
    {
        Response.Cookies.Append(AuthCookies.AccessToken, auth.Token, BuildOptions("/", auth.ExpiresAt));
        Response.Cookies.Append(
            AuthCookies.RefreshToken,
            auth.RefreshToken,
            BuildOptions(AuthCookies.RefreshPath, auth.RefreshExpiresAt));
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete(AuthCookies.AccessToken, BuildOptions("/", null));
        Response.Cookies.Delete(AuthCookies.RefreshToken, BuildOptions(AuthCookies.RefreshPath, null));
    }

    private CookieOptions BuildOptions(string path, DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = !_environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = path,
        Expires = expiresAt is null ? null : new DateTimeOffset(expiresAt.Value, TimeSpan.Zero)
    };
}
