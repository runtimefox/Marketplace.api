using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Sellers;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Sellers;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class SellerMutations
{
    [Authorize]
    public Task<SellerDto> CreateSeller(
        CreateSellerDto input,
        ClaimsPrincipal claimsPrincipal,
        ISellerService sellers,
        CancellationToken ct) =>
        sellers.CreateSellerAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<SellerDto?> UpdateSeller(
        Guid id,
        UpdateSellerDto input,
        ClaimsPrincipal claimsPrincipal,
        ISellerService sellers,
        CancellationToken ct) =>
        sellers.UpdateSellerAsync(claimsPrincipal.GetRequiredUserId(), id, input, ct);

    [Authorize]
    public Task<bool> DeleteSeller(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        ISellerService sellers,
        CancellationToken ct) =>
        sellers.DeleteSellerAsync(claimsPrincipal.GetRequiredUserId(), id, ct);

    [Authorize]
    public Task<SellerMemberDto> AddSellerManager(
        Guid sellerId,
        AddSellerManagerDto input,
        ClaimsPrincipal claimsPrincipal,
        ISellerMemberService members,
        CancellationToken ct) =>
        members.AddManagerAsync(claimsPrincipal.GetRequiredUserId(), sellerId, input, ct);

    [Authorize]
    public Task<bool> RemoveSellerMember(
        Guid sellerId,
        Guid userId,
        ClaimsPrincipal claimsPrincipal,
        ISellerMemberService members,
        CancellationToken ct) =>
        members.RemoveMemberAsync(claimsPrincipal.GetRequiredUserId(), sellerId, userId, ct);
}
