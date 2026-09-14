using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Cart;

public record UpdateCartItemDto
{
    public required Guid ProductId { get; init; }

    [Range(0, CartItem.MaxQuantity)]
    public required int Quantity { get; init; }
}
