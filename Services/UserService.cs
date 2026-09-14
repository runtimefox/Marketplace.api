using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Users;
using MyApi.Shared.Auth;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class UserService(AppDbContext dbContext, IPasswordHasher passwordHasher) : IUserService
{
    private const string UsernameOrEmailTaken = "This username or email is already taken.";

    public async Task<ActionResult<UserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await dbContext.UserAccounts
            .Where(x => x.Id == id)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync();

        return user is null ? new NotFoundResult() : user;
    }

    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        var users = await dbContext.UserAccounts
            .Select(UserDto.Projection)
            .ToListAsync();

        return users;
    }

    public async Task<ActionResult<UserDto>> CreateUserAsync(CreateUserDto createUser)
    {
        var passwordError = PasswordPolicy.Validate(createUser.Password);
        if (passwordError is not null)
        {
            return new BadRequestObjectResult(passwordError);
        }

        var conflict = await FindConflictAsync(null, createUser.Username, createUser.Email);
        if (conflict is not null)
        {
            return conflict;
        }

        var user = new UserAccount(
            createUser.Username,
            createUser.Email,
            passwordHasher.Hash(createUser.Password));

        dbContext.UserAccounts.Add(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return new ConflictObjectResult(UsernameOrEmailTaken);
        }

        return UserDto.FromEntity(user);
    }

    public async Task<ActionResult<UserDto>> UpdateProfileAsync(Guid id, UpdateProfileDto updateProfile)
    {
        var user = await dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundResult();
        }

        var conflict = await FindConflictAsync(id, updateProfile.Username, updateProfile.Email);
        if (conflict is not null)
        {
            return conflict;
        }

        user.ChangeUsername(updateProfile.Username);
        user.ChangeEmail(updateProfile.Email);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return new ConflictObjectResult(UsernameOrEmailTaken);
        }

        return UserDto.FromEntity(user);
    }

    private async Task<ActionResult?> FindConflictAsync(Guid? excludeUserId, string username, string email)
    {
        var usernameTaken = await dbContext.UserAccounts
            .AnyAsync(x => x.Id != excludeUserId && x.Username == username);
        if (usernameTaken)
        {
            return new ConflictObjectResult($"User \"{username}\" already exists.");
        }

        var emailTaken = await dbContext.UserAccounts
            .AnyAsync(x => x.Id != excludeUserId && x.Email == email);
        if (emailTaken)
        {
            return new ConflictObjectResult($"Email \"{email}\" is already taken.");
        }

        return null;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };
}
