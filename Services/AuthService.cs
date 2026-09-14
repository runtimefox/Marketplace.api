using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Auth;
using MyApi.Models.Dtos.Sellers;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Services.Interfaces.Users;
using MyApi.Shared.Auth;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "Refresh token is invalid.";

    private readonly AppDbContext _dbContext;
    private readonly IUserService _userService;
    private readonly ISellerService _sellerService;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        AppDbContext dbContext,
        IUserService userService,
        ISellerService sellerService,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _userService = userService;
        _sellerService = sellerService;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ActionResult<AuthResultDto>> RegisterAsync(CreateUserDto createUser)
    {
        var userResult = await _userService.CreateUserAsync(createUser);
        if (userResult.Result is not null)
        {
            return userResult.Result;
        }

        return await IssueTokensAsync(userResult.Value!);
    }

    public async Task<ActionResult<AuthResultDto>> RegisterSellerAsync(RegisterSellerDto registerSeller)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var userResult = await _userService.CreateUserAsync(registerSeller);
        if (userResult.Result is not null)
        {
            return userResult.Result;
        }

        var user = userResult.Value!;
        await _sellerService.CreateSellerAsync(user.Id, new CreateSellerDto { Name = registerSeller.SellerName });
        var tokens = await IssueTokensAsync(user);

        await transaction.CommitAsync();

        return tokens;
    }

    public async Task<ActionResult<AuthResultDto>> LoginAsync(LoginDto login)
    {
        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(x => x.Email == login.Email);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, login.Password))
        {
            return new UnauthorizedObjectResult(InvalidCredentials);
        }

        return await IssueTokensAsync(UserDto.FromEntity(user));
    }

    public async Task<ActionResult<AuthResultDto>> RefreshAsync(string refreshToken)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);

        var stored = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (stored is null)
        {
            return new UnauthorizedObjectResult(InvalidRefreshToken);
        }

        if (!stored.IsActive)
        {
            await RevokeAllForUserAsync(stored.UserId);
            return new UnauthorizedObjectResult(InvalidRefreshToken);
        }

        stored.Revoke();

        return await IssueTokensAsync(UserDto.FromEntity(stored.User));
    }

    public async Task<ActionResult> RevokeAsync(string refreshToken)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);

        var stored = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash);
        
        if (stored is not null && stored.IsActive)
        {
            stored.Revoke();
            await _dbContext.SaveChangesAsync();
        }

        return new NoContentResult();
    }

    public async Task<ActionResult<AuthResultDto>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePassword)
    {
        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            return new UnauthorizedObjectResult("User not found.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, changePassword.CurrentPassword))
        {
            return new BadRequestObjectResult("Current password is incorrect.");
        }

        if (changePassword.NewPassword == changePassword.CurrentPassword)
        {
            return new BadRequestObjectResult("New password must differ from the current one.");
        }

        var passwordError = PasswordPolicy.Validate(changePassword.NewPassword);
        if (passwordError is not null)
        {
            return new BadRequestObjectResult(passwordError);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        user.SetPasswordHash(_passwordHasher.Hash(changePassword.NewPassword));
        await _dbContext.RefreshTokens
            .Where(x => x.UserId == user.Id)
            .ExecuteDeleteAsync();
        var tokens = await IssueTokensAsync(UserDto.FromEntity(user));

        await transaction.CommitAsync();

        return tokens;
    }

    private async Task<AuthResultDto> IssueTokensAsync(UserDto user)
    {
        var access = _tokenService.GenerateToken(user);
        var refresh = _tokenService.GenerateRefreshToken();

        _dbContext.RefreshTokens.Add(new RefreshToken(user.Id, refresh.Hash, refresh.ExpiresAt));
        await _dbContext.SaveChangesAsync();

        return new AuthResultDto
        {
            Token = access.Value,
            ExpiresAt = access.ExpiresAt,
            RefreshToken = refresh.Value,
            RefreshExpiresAt = refresh.ExpiresAt
        };
    }

    private async Task RevokeAllForUserAsync(Guid userId)
    {
        var active = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync();

        foreach (var token in active)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync();
    }

}
