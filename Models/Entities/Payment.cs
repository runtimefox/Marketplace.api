using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Payments")]
public class Payment
{
    public const int MaxOrders = 50;
    public const int CurrencyLength = 3;
    public const int ProviderPaymentIdMaxLength = 255;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("BuyerId")]
    public Guid BuyerId { get; private set; }

    public UserAccount Buyer { get; private set; } = null!;

    [Column("Status")]
    public PaymentStatus Status { get; private set; }

    [Column("Amount")]
    public decimal Amount { get; private set; }

    [Required]
    [MaxLength(CurrencyLength)]
    [Column("Currency")]
    public string Currency { get; private set; } = null!;

    [Required]
    [MaxLength(ProviderPaymentIdMaxLength)]
    [Column("ProviderPaymentId")]
    public string ProviderPaymentId { get; private set; } = null!;

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    [Column("SucceededAt")]
    public DateTimeOffset? SucceededAt { get; private set; }

    [Column("CanceledAt")]
    public DateTimeOffset? CanceledAt { get; private set; }

    private readonly List<Order> _orders = [];

    public IReadOnlyCollection<Order> Orders => _orders;

    private Payment()
    {
    }

    public static Payment Create(Guid buyerId, string currency, IReadOnlyCollection<Order> orders)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer is required.", nameof(buyerId));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != CurrencyLength)
        {
            throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(currency));
        }

        ArgumentNullException.ThrowIfNull(orders);

        if (orders.Count == 0)
        {
            throw new ArgumentException("A payment must cover at least one order.", nameof(orders));
        }

        if (orders.Count > MaxOrders)
        {
            throw new ArgumentException($"A payment can cover at most {MaxOrders} orders.", nameof(orders));
        }

        if (orders.Any(x => x.BuyerId != buyerId))
        {
            throw new InvalidOperationException("A payment can cover only orders of the same buyer.");
        }

        var now = DateTimeOffset.UtcNow;
        var payment = new Payment
        {
            Id = Guid.CreateVersion7(),
            BuyerId = buyerId,
            Status = PaymentStatus.Pending,
            Currency = currency.Trim().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var order in orders)
        {
            order.AttachPayment(payment, now);
            payment._orders.Add(order);
            payment.Amount += order.TotalAmount;
        }

        return payment;
    }

    public void AttachProviderPayment(string providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new ArgumentException("Provider payment id is required.", nameof(providerPaymentId));
        }

        if (providerPaymentId.Length > ProviderPaymentIdMaxLength)
        {
            throw new ArgumentException(
                $"Provider payment id must not exceed {ProviderPaymentIdMaxLength} characters.", nameof(providerPaymentId));
        }

        if (ProviderPaymentId is not null)
        {
            throw new InvalidOperationException("The payment is already linked to the payment provider.");
        }

        ProviderPaymentId = providerPaymentId;
    }

    public void MarkSucceeded(decimal amountReceived, string currency)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        if (Status == PaymentStatus.Canceled)
        {
            throw new InvalidOperationException("A canceled payment cannot succeed.");
        }

        if (amountReceived != Amount || !string.Equals(currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The received amount does not match the payment.");
        }

        if (_orders.Count == 0)
        {
            throw new InvalidOperationException("Payment orders must be loaded to complete a payment.");
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var order in _orders)
        {
            order.MarkPaid(now);
        }

        Status = PaymentStatus.Succeeded;
        SucceededAt = UpdatedAt = now;
    }

    public void Cancel()
    {
        if (Status == PaymentStatus.Canceled)
        {
            return;
        }

        if (Status == PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("A succeeded payment cannot be canceled.");
        }

        Status = PaymentStatus.Canceled;
        CanceledAt = UpdatedAt = DateTimeOffset.UtcNow;
    }
}
