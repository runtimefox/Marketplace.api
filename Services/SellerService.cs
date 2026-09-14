using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Sellers;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class SellerService : ISellerService
{
    private readonly AppDbContext _dbContext;
    private readonly ISellerAccessService _sellerAccess;

    public SellerService(AppDbContext dbContext, ISellerAccessService sellerAccess)
    {
        _dbContext = dbContext;
        _sellerAccess = sellerAccess;
    }

    public IQueryable<SellerDto> Query() =>
        _dbContext.Sellers.Select(SellerDto.Projection);

    public IQueryable<SellerMembershipDto> QueryMemberships(Guid userId) =>
        _dbContext.SellerMembers
            .Where(x => x.UserAccountId == userId)
            .Select(SellerMembershipDto.Projection);

    public Task<SellerDto?> GetSellerByIdAsync(Guid id, CancellationToken ct = default) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<SellerDto> CreateSellerAsync(
        Guid userId, CreateSellerDto dto, CancellationToken ct = default)
    {
        var seller = new Seller(dto.Name, userId);

        _dbContext.Sellers.Add(seller);
        await _dbContext.SaveChangesAsync(ct);

        return SellerDto.FromEntity(seller);
    }

    public async Task<SellerDto?> UpdateSellerAsync(
        Guid userId, Guid id, UpdateSellerDto dto, CancellationToken ct = default)
    {
        var seller = await _dbContext.Sellers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (seller is null)
        {
            return null;
        }

        await _sellerAccess.EnsureOwnerAsync(userId, id, ct);

        seller.Rename(dto.Name);
        await _dbContext.SaveChangesAsync(ct);

        return SellerDto.FromEntity(seller);
    }

    public async Task<bool> DeleteSellerAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var seller = await _dbContext.Sellers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (seller is null)
        {
            return false;
        }

        await _sellerAccess.EnsureOwnerAsync(userId, id, ct);

        var hasProducts = await _dbContext.Products.AnyAsync(x => x.SellerId == id, ct);
        if (hasProducts)
        {
            throw new InvalidOperationException(HasProductsMessage(id));
        }

        _dbContext.Sellers.Remove(seller);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            throw new InvalidOperationException(HasProductsMessage(id));
        }

        return true;
    }

    private static string HasProductsMessage(Guid id) =>
        $"Seller {id} has products and cannot be deleted.";
}
