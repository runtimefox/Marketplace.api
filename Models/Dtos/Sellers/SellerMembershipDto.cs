using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record SellerMembershipDto
{
    public required SellerDto Seller { get; init; }
    public required SellerMemberRole Role { get; init; }

    public static Expression<Func<SellerMember, SellerMembershipDto>> Projection => x => new SellerMembershipDto
    {
        Seller = new SellerDto
        {
            Id = x.Seller.Id,
            Name = x.Seller.Name,
            Rating = x.Seller.Rating,
            LogoKey = x.Seller.LogoKey
        },
        Role = x.Role
    };
}
