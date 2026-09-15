using MyApi.Models.Dtos.Images;
using MyApi.Models.Dtos.Users;
using MyApi.Services.Interfaces.Images;

namespace MyApi.GraphQL.Images;

[ExtendObjectType<UserSummaryDto>]
public class UserSummaryAvatarField
{
    public ImageDto? GetAvatar([Parent] UserSummaryDto user, IImageUrlBuilder imageUrls) =>
        user.AvatarKey is null ? null : imageUrls.Build(user.AvatarKey);
}
