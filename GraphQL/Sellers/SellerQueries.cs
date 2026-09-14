using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Sellers;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Sellers;

[ExtendObjectType(OperationTypeNames.Query)]
public class SellerQueries
{
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<SellerDto> GetSellers(ISellerService sellers) =>
        sellers.Query();

    public Task<SellerDto?> GetSellerById(
        Guid id,
        ISellerService sellers,
        CancellationToken ct) =>
        sellers.GetSellerByIdAsync(id, ct);

    [Authorize]
    public IQueryable<SellerMembershipDto> GetMySellers(
        ClaimsPrincipal claimsPrincipal,
        ISellerService sellers) =>
        sellers.QueryMemberships(claimsPrincipal.GetRequiredUserId());

    [Authorize]
    public Task<IQueryable<SellerMemberDto>> GetSellerMembers(
        Guid sellerId,
        ClaimsPrincipal claimsPrincipal,
        ISellerMemberService members,
        CancellationToken ct) =>
        members.QueryMembersAsync(claimsPrincipal.GetRequiredUserId(), sellerId, ct);
}
