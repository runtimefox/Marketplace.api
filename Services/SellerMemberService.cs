using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Sellers;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class SellerMemberService : ISellerMemberService
{
    private readonly AppDbContext _dbContext;
    private readonly ISellerAccessService _sellerAccess;

    public SellerMemberService(AppDbContext dbContext, ISellerAccessService sellerAccess)
    {
        _dbContext = dbContext;
        _sellerAccess = sellerAccess;
    }

    public async Task<IQueryable<SellerMemberDto>> QueryMembersAsync(
        Guid userId, Guid sellerId, CancellationToken ct = default)
    {
        await _sellerAccess.EnsureMemberAsync(userId, sellerId, ct);

        return QueryMembers(sellerId);
    }

    public async Task<SellerMemberDto> AddManagerAsync(
        Guid userId, Guid sellerId, AddSellerManagerDto dto, CancellationToken ct = default)
    {
        await _sellerAccess.EnsureOwnerAsync(userId, sellerId, ct);

        var email = dto.Email.Trim();
        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Email == email, ct)
                   ?? throw new InvalidOperationException($"User with email {email} was not found.");

        var seller = await _dbContext.Sellers
            .Include(x => x.Members)
            .FirstAsync(x => x.Id == sellerId, ct);

        var member = seller.AddManager(user.Id);
        _dbContext.SellerMembers.Add(member);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException("User is already a member of this seller.");
        }

        return await QueryMembers(sellerId).FirstAsync(x => x.UserId == user.Id, ct);
    }

    public async Task<bool> RemoveMemberAsync(
        Guid userId, Guid sellerId, Guid memberUserId, CancellationToken ct = default)
    {
        if (memberUserId == userId)
        {
            await _sellerAccess.EnsureMemberAsync(userId, sellerId, ct);
        }
        else
        {
            await _sellerAccess.EnsureOwnerAsync(userId, sellerId, ct);
        }

        var seller = await _dbContext.Sellers
            .Include(x => x.Members)
            .FirstAsync(x => x.Id == sellerId, ct);

        if (seller.Members.All(x => x.UserAccountId != memberUserId))
        {
            return false;
        }

        var member = seller.RemoveMember(memberUserId);
        _dbContext.SellerMembers.Remove(member);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    private IQueryable<SellerMemberDto> QueryMembers(Guid sellerId) =>
        _dbContext.SellerMembers
            .Where(x => x.SellerId == sellerId)
            .Select(SellerMemberDto.Projection);
}
