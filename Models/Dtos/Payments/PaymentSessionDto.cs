namespace MyApi.Models.Dtos.Payments;

public record PaymentSessionDto
{
    public required PaymentDto Payment { get; init; }
    public required string ClientSecret { get; init; }
}
