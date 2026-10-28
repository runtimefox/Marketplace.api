using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Images;
using MyApi.Services.Interfaces.Images;
using MyApi.Shared.Auth;

namespace MyApi.Controllers;

[ApiController]
[Authorize]
[Route("api/auth/me/avatar")]
public class AvatarController(IProfileImageService profileImages) : ControllerBase
{
    [HttpPut]
    [RequestSizeLimit(ImageUploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxRequestBytes)]
    public async Task<ActionResult<ImageDto>> Upload(IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();

        return await profileImages.SetAvatarAsync(
            User.GetRequiredUserId(), new ImageUpload(content, file.ContentType, file.Length), ct);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct) =>
        await profileImages.DeleteAvatarAsync(User.GetRequiredUserId(), ct)
            ? NoContent()
            : NotFound();
}
