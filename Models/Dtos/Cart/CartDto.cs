namespace MyApi.Models.Dtos.Cart;

public record CartDto
{
    public required IReadOnlyList<CartItemDto> Items { get; init; }
    public required int TotalQuantity { get; init; }
    public required decimal TotalPrice { get; init; }

    public static CartDto FromItems(IReadOnlyList<CartItemDto> items) => new()
    {
        Items = items,
        TotalQuantity = items.Sum(x => x.Quantity),
        TotalPrice = items.Sum(x => x.LineTotal)
    };
}
