using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Cart;

public record AddToCartDto
{
    public required Guid ProductId { get; init; }

    [Range(1, CartItem.MaxQuantity)]
    public int Quantity { get; init; } = 1;
}
