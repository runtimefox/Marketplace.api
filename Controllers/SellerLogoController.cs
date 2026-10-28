using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Images;
using MyApi.Services.Interfaces.Images;
using MyApi.Shared.Auth;

namespace MyApi.Controllers;

[ApiController]
[Authorize]
[Route("api/sellers/{sellerId:guid}/logo")]
public class SellerLogoController(IProfileImageService profileImages) : ControllerBase
{
    [HttpPut]
    [RequestSizeLimit(ImageUploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxRequestBytes)]
    public async Task<ActionResult<ImageDto>> Upload(Guid sellerId, IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();

        var logo = await profileImages.SetSellerLogoAsync(
            User.GetRequiredUserId(), sellerId, new ImageUpload(content, file.ContentType, file.Length), ct);

        return logo is null ? NotFound() : Ok(logo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid sellerId, CancellationToken ct) =>
        await profileImages.DeleteSellerLogoAsync(User.GetRequiredUserId(), sellerId, ct)
            ? NoContent()
            : NotFound();
}
