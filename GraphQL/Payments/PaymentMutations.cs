using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Payments;
using MyApi.Services.Interfaces.Payments;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Payments;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class PaymentMutations
{
    [Authorize]
    public Task<PaymentSessionDto> CreatePayment(
        CreatePaymentDto input,
        ClaimsPrincipal claimsPrincipal,
        IPaymentService payments,
        CancellationToken ct) =>
        payments.CreatePaymentAsync(claimsPrincipal.GetRequiredUserId(), input, ct);
}
