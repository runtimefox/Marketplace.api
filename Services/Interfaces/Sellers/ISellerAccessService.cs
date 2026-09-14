namespace MyApi.Services.Interfaces.Sellers;

public interface ISellerAccessService
{
    Task<bool> IsMemberAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
    Task EnsureMemberAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
    Task EnsureOwnerAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
}
