using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Payments;

public record PaymentDto
{
    public required Guid Id { get; init; }
    public required PaymentStatus Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyList<Guid> OrderIds { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SucceededAt { get; init; }
    public DateTimeOffset? CanceledAt { get; init; }

    public static Expression<Func<Payment, PaymentDto>> Projection => x => new PaymentDto
    {
        Id = x.Id,
        Status = x.Status,
        Amount = x.Amount,
        Currency = x.Currency,
        OrderIds = x.Orders.OrderBy(o => o.CreatedAt).Select(o => o.Id).ToList(),
        CreatedAt = x.CreatedAt,
        SucceededAt = x.SucceededAt,
        CanceledAt = x.CanceledAt
    };
}
