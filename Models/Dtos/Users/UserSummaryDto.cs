using HotChocolate;

namespace MyApi.Models.Dtos.Users;

public record UserSummaryDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }

    [GraphQLIgnore]
    public string? AvatarKey { get; init; }
}
