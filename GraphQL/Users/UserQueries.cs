using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Shared.Auth;
using MyApi.Shared.Data;

namespace MyApi.GraphQL.Users;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    [Authorize(Roles = new[] { nameof(UserRole.Admin) })]
    public IQueryable<UserDto> GetUsers(AppDbContext dbContext) =>
        dbContext.UserAccounts.Select(UserDto.Projection);

    [Authorize]
    public Task<UserDto?> GetMe(ClaimsPrincipal claimsPrincipal, AppDbContext dbContext, CancellationToken ct)
    {
        var userId = claimsPrincipal.GetRequiredUserId();

        return dbContext.UserAccounts
            .Where(x => x.Id == userId)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync(ct);
    }
}
