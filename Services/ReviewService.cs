using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Reviews;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Reviews;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class ReviewService(AppDbContext dbContext, ISellerAccessService sellerAccess) : IReviewService
{
    private const string AlreadyReviewed = "You have already reviewed this product.";

    public IQueryable<ReviewDto> QueryProductReviews(Guid productId) =>
        QueryReviews(x => x.ProductId == productId);

    public IQueryable<ReviewDto> QueryUserReviews(Guid userId) =>
        QueryReviews(x => x.UserAccountId == userId);

    public async Task<bool> CanReviewAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var sellerId = await FindSellerIdAsync(productId, ct);

        return sellerId is not null
               && await FindReviewBlockerAsync(userId, productId, sellerId.Value, ct) is null;
    }

    public async Task<ReviewDto> CreateReviewAsync(Guid userId, CreateReviewDto dto, CancellationToken ct = default)
    {
        var sellerId = await FindSellerIdAsync(dto.ProductId, ct)
                       ?? throw new InvalidOperationException($"Product {dto.ProductId} was not found.");

        var blocker = await FindReviewBlockerAsync(userId, dto.ProductId, sellerId, ct);
        if (blocker is not null)
        {
            throw blocker;
        }

        var review = new Review(dto.ProductId, userId, dto.Rating, dto.Comment);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        dbContext.Reviews.Add(review);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException(AlreadyReviewed);
        }

        await RecalculateSellerRatingAsync(sellerId, ct);
        await transaction.CommitAsync(ct);

        return await GetReviewDtoAsync(review.Id, ct);
    }

    public async Task<ReviewDto?> UpdateReviewAsync(
        Guid userId, Guid id, UpdateReviewDto dto, CancellationToken ct = default)
    {
        var review = await dbContext.Reviews
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (review is null)
        {
            return null;
        }

        if (review.UserAccountId != userId)
        {
            throw new UnauthorizedAccessException("Only the author can edit a review.");
        }

        review.Update(dto.Rating, dto.Comment);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.SaveChangesAsync(ct);
        await RecalculateSellerRatingAsync(review.Product.SellerId, ct);
        await transaction.CommitAsync(ct);

        return await GetReviewDtoAsync(id, ct);
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, bool isAdmin, Guid id, CancellationToken ct = default)
    {
        var review = await dbContext.Reviews
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (review is null)
        {
            return false;
        }

        if (review.UserAccountId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("Only the author or an admin can delete a review.");
        }

        var sellerId = review.Product.SellerId;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        dbContext.Reviews.Remove(review);
        await dbContext.SaveChangesAsync(ct);
        await RecalculateSellerRatingAsync(sellerId, ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    private async Task<Exception?> FindReviewBlockerAsync(
        Guid userId, Guid productId, Guid sellerId, CancellationToken ct)
    {
        if (await sellerAccess.IsMemberAsync(userId, sellerId, ct))
        {
            return new UnauthorizedAccessException("Shop members cannot review their own products.");
        }

        var purchased = await dbContext.Orders.AnyAsync(
            x => x.BuyerId == userId
                 && x.Status == OrderStatus.Delivered
                 && x.Items.Any(i => i.ProductId == productId),
            ct);

        if (!purchased)
        {
            return new UnauthorizedAccessException("You can review only products from your delivered orders.");
        }

        var alreadyReviewed = await dbContext.Reviews
            .AnyAsync(x => x.ProductId == productId && x.UserAccountId == userId, ct);

        return alreadyReviewed ? new InvalidOperationException(AlreadyReviewed) : null;
    }

    private Task<Guid?> FindSellerIdAsync(Guid productId, CancellationToken ct) =>
        dbContext.Products
            .Where(x => x.Id == productId)
            .Select(x => (Guid?)x.SellerId)
            .FirstOrDefaultAsync(ct);

    private Task RecalculateSellerRatingAsync(Guid sellerId, CancellationToken ct) =>
        dbContext.Sellers
            .Where(x => x.Id == sellerId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.Rating,
                    x => Math.Round(
                        dbContext.Reviews
                            .Where(r => r.Product.SellerId == x.Id)
                            .Average(r => (decimal?)r.Rating) ?? Seller.MinRating,
                        2)),
                ct);

    private Task<ReviewDto> GetReviewDtoAsync(Guid id, CancellationToken ct) =>
        QueryReviews(x => x.Id == id).FirstAsync(ct);

    private IQueryable<ReviewDto> QueryReviews(Expression<Func<Review, bool>> predicate) =>
        dbContext.Reviews
            .Where(predicate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ReviewDto.Projection);
}
