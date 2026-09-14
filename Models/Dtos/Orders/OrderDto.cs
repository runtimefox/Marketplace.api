using System.Linq.Expressions;
using MyApi.Models.Dtos.Sellers;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Orders;

public record OrderDto
{
    public required Guid Id { get; init; }
    public required OrderStatus Status { get; init; }
    public required decimal TotalAmount { get; init; }
    public required UserSummaryDto Buyer { get; init; }
    public required SellerDto Seller { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
    public required DeliveryAddressDto DeliveryAddress { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ShippedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? CanceledAt { get; init; }

    public static Expression<Func<Order, OrderDto>> Projection => x => new OrderDto
    {
        Id = x.Id,
        Status = x.Status,
        TotalAmount = x.TotalAmount,
        Buyer = new UserSummaryDto
        {
            Id = x.Buyer.Id,
            Username = x.Buyer.Username
        },
        Seller = new SellerDto
        {
            Id = x.Seller.Id,
            Name = x.Seller.Name,
            Rating = x.Seller.Rating
        },
        Items = x.Items
            .Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSku = i.ProductSku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.UnitPrice * i.Quantity
            })
            .ToList(),
        DeliveryAddress = new DeliveryAddressDto
        {
            RecipientName = x.DeliveryAddress.RecipientName,
            Phone = x.DeliveryAddress.Phone,
            Country = x.DeliveryAddress.Country,
            City = x.DeliveryAddress.City,
            AddressLine = x.DeliveryAddress.AddressLine,
            Apartment = x.DeliveryAddress.Apartment,
            PostalCode = x.DeliveryAddress.PostalCode,
            Comment = x.DeliveryAddress.Comment
        },
        CreatedAt = x.CreatedAt,
        ShippedAt = x.ShippedAt,
        DeliveredAt = x.DeliveredAt,
        CanceledAt = x.CanceledAt
    };
}
