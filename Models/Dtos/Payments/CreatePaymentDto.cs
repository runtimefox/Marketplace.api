using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Payments;

public record CreatePaymentDto
{
    [MinLength(1)]
    [MaxLength(Payment.MaxOrders)]
    public required IReadOnlyList<Guid> OrderIds { get; init; }
}
