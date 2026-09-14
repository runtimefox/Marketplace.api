using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record SellerMembershipDto
{
    public required Guid SellerId { get; init; }
    public required string SellerName { get; init; }
    public required decimal SellerRating { get; init; }
    public required SellerMemberRole Role { get; init; }

    public static Expression<Func<SellerMember, SellerMembershipDto>> Projection => x => new SellerMembershipDto
    {
        SellerId = x.SellerId,
        SellerName = x.Seller.Name,
        SellerRating = x.Seller.Rating,
        Role = x.Role
    };
}
