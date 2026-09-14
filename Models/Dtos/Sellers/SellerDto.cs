using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record SellerDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal Rating { get; init; }

    public static Expression<Func<Seller, SellerDto>> Projection => x => new SellerDto
    {
        Id = x.Id,
        Name = x.Name,
        Rating = x.Rating
    };

    public static SellerDto FromEntity(Seller seller) => new()
    {
        Id = seller.Id,
        Name = seller.Name,
        Rating = seller.Rating
    };
}
