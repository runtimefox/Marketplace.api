using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Users;

public record UserDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }

    public static Expression<Func<UserAccount, UserDto>> Projection => x => new UserDto
    {
        Id = x.Id,
        Username = x.Username,
        Email = x.Email,
        Role = x.Role
    };

    public static UserDto FromEntity(UserAccount user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role
    };
}
