using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Auth;
using MyApi.Models.Dtos.Users;

namespace MyApi.Services.Interfaces.Auth;

public interface IAuthService
{
    Task<ActionResult<AuthResultDto>> RegisterAsync(CreateUserDto createUser);
    Task<ActionResult<AuthResultDto>> RegisterSellerAsync(RegisterSellerDto registerSeller);
    Task<ActionResult<AuthResultDto>> LoginAsync(LoginDto login);
    Task<ActionResult<AuthResultDto>> RefreshAsync(string refreshToken);
    Task<ActionResult> RevokeAsync(string refreshToken);
}
