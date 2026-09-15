using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Images;
using MyApi.Services.Interfaces.Images;
using MyApi.Shared.Auth;

namespace MyApi.Controllers;

[ApiController]
[Authorize]
[Route("api/products/{productId:guid}/images")]
public class ProductImagesController(IProductImageService productImages) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxRequestBytes)]
    public async Task<ActionResult<ProductImageResponseDto>> Upload(Guid productId, IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();

        var image = await productImages.AddAsync(
            User.GetRequiredUserId(), productId, new ImageUpload(content, file.ContentType, file.Length), ct);

        return image is null ? NotFound() : StatusCode(StatusCodes.Status201Created, image);
    }

    [HttpPut("order")]
    public async Task<ActionResult<IReadOnlyList<ProductImageResponseDto>>> Reorder(
        Guid productId, ReorderProductImagesDto reorder, CancellationToken ct)
    {
        var images = await productImages.ReorderAsync(User.GetRequiredUserId(), productId, reorder.ImageIds, ct);

        return images is null ? NotFound() : Ok(images);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid imageId, CancellationToken ct) =>
        await productImages.DeleteAsync(User.GetRequiredUserId(), productId, imageId, ct)
            ? NoContent()
            : NotFound();
}
