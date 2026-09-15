using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.Dtos.Images;

public record ReorderProductImagesDto
{
    [Required]
    public required List<Guid> ImageIds { get; init; }
}
