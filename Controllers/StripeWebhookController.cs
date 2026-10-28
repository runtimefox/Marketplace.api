using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Services.Interfaces.Payments;

namespace MyApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/payments/stripe/webhook")]
public class StripeWebhookController(IPaymentService payments) : ControllerBase
{
    public const string SignatureHeader = "Stripe-Signature";
    private const int MaxPayloadBytes = 512 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxPayloadBytes)]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(SignatureHeader, out var signature) || string.IsNullOrEmpty(signature))
        {
            return BadRequest();
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        await payments.HandleWebhookAsync(payload, signature.ToString(), ct);

        return Ok();
    }
}
