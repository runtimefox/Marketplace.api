using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Users;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<ActionResult<UserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _dbContext.UserAccounts
            .Where(x => x.Id == id)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync();

        return user is null ? new NotFoundResult() : user;
    }

    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        var users = await _dbContext.UserAccounts
            .Select(UserDto.Projection)
            .ToListAsync();

        return users;
    }

    public async Task<ActionResult<UserDto>> CreateUserAsync(CreateUserDto createUser)
    {
        var invalidPassword = ValidatePassword(createUser.Password);
        if (invalidPassword is not null)
        {
            return invalidPassword;
        }

        var usernameTaken = await _dbContext.UserAccounts
            .AnyAsync(x => x.Username == createUser.Username);
        if (usernameTaken)
        {
            return new ConflictObjectResult($"User \"{createUser.Username}\" already exists.");
        }

        var emailTaken = await _dbContext.UserAccounts
            .AnyAsync(x => x.Email == createUser.Email);
        if (emailTaken)
        {
            return new ConflictObjectResult($"Email \"{createUser.Email}\" is already taken.");
        }

        var user = new UserAccount(
            createUser.Username,
            createUser.Email,
            _passwordHasher.Hash(createUser.Password));

        _dbContext.UserAccounts.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return new ConflictObjectResult("This username or email is already taken.");
        }

        return UserDto.FromEntity(user);
    }

    public async Task<ActionResult<UserDto>> UpdateUserAsync(Guid id, CreateUserDto updateUser)
    {
        var invalidPassword = ValidatePassword(updateUser.Password);
        if (invalidPassword is not null)
        {
            return invalidPassword;
        }

        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundResult();
        }

        user.ChangeUsername(updateUser.Username);
        user.ChangeEmail(updateUser.Email);
        user.SetPasswordHash(_passwordHasher.Hash(updateUser.Password));
        await _dbContext.SaveChangesAsync();

        return UserDto.FromEntity(user);
    }

    public async Task<ActionResult<UserDto>> DeleteUserAsync(Guid id)
    {
        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundResult();
        }

        _dbContext.UserAccounts.Remove(user);
        await _dbContext.SaveChangesAsync();

        return UserDto.FromEntity(user);
    }

    private static ActionResult? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < UserAccount.PasswordMinLength)
        {
            return new BadRequestObjectResult(
                $"Password must be at least {UserAccount.PasswordMinLength} characters long.");
        }

        if (password.Length > UserAccount.RawPasswordMaxLength)
        {
            return new BadRequestObjectResult(
                $"Password must not exceed {UserAccount.RawPasswordMaxLength} characters.");
        }

        return null;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };

}
