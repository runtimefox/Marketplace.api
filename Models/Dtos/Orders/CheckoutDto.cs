namespace MyApi.Models.Dtos.Orders;

public record CheckoutDto
{
    public required DeliveryAddressDto DeliveryAddress { get; init; }
}
