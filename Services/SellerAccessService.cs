using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class SellerAccessService : ISellerAccessService
{
    private readonly AppDbContext _dbContext;

    public SellerAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsMemberAsync(Guid userId, Guid sellerId, CancellationToken ct = default) =>
        _dbContext.SellerMembers.AnyAsync(x => x.SellerId == sellerId && x.UserAccountId == userId, ct);

    public Task EnsureMemberAsync(Guid userId, Guid sellerId, CancellationToken ct = default) =>
        GetRoleAsync(userId, sellerId, ct);

    public async Task EnsureOwnerAsync(Guid userId, Guid sellerId, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(userId, sellerId, ct);

        if (role != SellerMemberRole.Owner)
        {
            throw new UnauthorizedAccessException(
                $"Only the owner of seller {sellerId} can perform this action.");
        }
    }

    private async Task<SellerMemberRole> GetRoleAsync(Guid userId, Guid sellerId, CancellationToken ct)
    {
        var role = await _dbContext.SellerMembers
            .Where(x => x.SellerId == sellerId && x.UserAccountId == userId)
            .Select(x => (SellerMemberRole?)x.Role)
            .FirstOrDefaultAsync(ct);

        if (role is not null)
        {
            return role.Value;
        }

        var sellerExists = await _dbContext.Sellers.AnyAsync(x => x.Id == sellerId, ct);

        if (!sellerExists)
        {
            throw new InvalidOperationException($"Seller {sellerId} was not found.");
        }

        throw new UnauthorizedAccessException($"You are not a member of seller {sellerId}.");
    }
}
