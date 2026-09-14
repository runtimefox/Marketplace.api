using MyApi.Models.Dtos.Sellers;

namespace MyApi.Services.Interfaces.Sellers;

public interface ISellerService
{
    IQueryable<SellerDto> Query();
    IQueryable<SellerMembershipDto> QueryMemberships(Guid userId);
    Task<SellerDto?> GetSellerByIdAsync(Guid id, CancellationToken ct = default);
    Task<SellerDto> CreateSellerAsync(Guid userId, CreateSellerDto dto, CancellationToken ct = default);
    Task<SellerDto?> UpdateSellerAsync(Guid userId, Guid id, UpdateSellerDto dto, CancellationToken ct = default);
    Task<bool> DeleteSellerAsync(Guid userId, Guid id, CancellationToken ct = default);
}
