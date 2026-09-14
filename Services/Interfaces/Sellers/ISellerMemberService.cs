using MyApi.Models.Dtos.Sellers;

namespace MyApi.Services.Interfaces.Sellers;

public interface ISellerMemberService
{
    Task<IQueryable<SellerMemberDto>> QueryMembersAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
    Task<SellerMemberDto> AddManagerAsync(Guid userId, Guid sellerId, AddSellerManagerDto dto, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid userId, Guid sellerId, Guid memberUserId, CancellationToken ct = default);
}
