using MyApi.Models.Dtos.Images;
using MyApi.Models.Dtos.Users;
using MyApi.Services.Interfaces.Images;

namespace MyApi.GraphQL.Images;

[ExtendObjectType<UserDto>]
public class UserAvatarField
{
    public ImageDto? GetAvatar([Parent] UserDto user, IImageUrlBuilder imageUrls) =>
        user.AvatarKey is null ? null : imageUrls.Build(user.AvatarKey);
}
