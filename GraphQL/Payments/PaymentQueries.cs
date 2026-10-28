using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Payments;
using MyApi.Services.Interfaces.Payments;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Payments;

[ExtendObjectType(OperationTypeNames.Query)]
public class PaymentQueries
{
    [Authorize]
    public Task<PaymentDto?> GetPaymentById(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IPaymentService payments,
        CancellationToken ct) =>
        payments.GetPaymentByIdAsync(claimsPrincipal.GetRequiredUserId(), id, ct);
}
