namespace MyApi.Services.Interfaces.Sellers;

public interface ISellerAccessService
{
    Task EnsureMemberAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
    Task EnsureOwnerAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
}
