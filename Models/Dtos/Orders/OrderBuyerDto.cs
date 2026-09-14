namespace MyApi.Models.Dtos.Orders;

public record OrderBuyerDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
}
