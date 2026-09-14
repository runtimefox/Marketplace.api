using HotChocolate.Authorization;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Shared.Data;

namespace MyApi.GraphQL.Users;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    [Authorize(Roles = new[] { nameof(UserRole.Admin) })]
    public IQueryable<UserDto> GetUsers(AppDbContext dbContext) =>
        dbContext.UserAccounts.Select(UserDto.Projection);
}
