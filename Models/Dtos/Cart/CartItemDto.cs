using MyApi.Models.Dtos.Products;

namespace MyApi.Models.Dtos.Cart;

public record CartItemDto
{
    public required ProductDto Product { get; init; }
    public required int Quantity { get; init; }
    public required decimal LineTotal { get; init; }
    public required bool IsAvailable { get; init; }

    public static CartItemDto Create(ProductDto product, int quantity) => new()
    {
        Product = product,
        Quantity = quantity,
        LineTotal = product.Price * quantity,
        IsAvailable = product.IsActive && product.Stock >= quantity
    };
}
