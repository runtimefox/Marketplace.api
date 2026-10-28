using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Orders")]
public class Order
{
    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("BuyerId")]
    public Guid BuyerId { get; private set; }

    public UserAccount Buyer { get; private set; } = null!;

    [Column("SellerId")]
    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    [Column("Status")]
    public OrderStatus Status { get; private set; }

    [Column("TotalAmount")]
    public decimal TotalAmount { get; private set; }

    public DeliveryAddress DeliveryAddress { get; private set; } = null!;

    [Column("PaymentId")]
    public Guid? PaymentId { get; private set; }

    public Payment? Payment { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    [Column("PaidAt")]
    public DateTimeOffset? PaidAt { get; private set; }

    [Column("ShippedAt")]
    public DateTimeOffset? ShippedAt { get; private set; }

    [Column("DeliveredAt")]
    public DateTimeOffset? DeliveredAt { get; private set; }

    [Column("CanceledAt")]
    public DateTimeOffset? CanceledAt { get; private set; }

    public uint Version { get; private set; }

    private readonly List<OrderItem> _items = [];

    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order()
    {
    }

    public static Order Create(
        Guid buyerId,
        Guid sellerId,
        DeliveryAddress deliveryAddress,
        IEnumerable<(Product Product, int Quantity)> lines)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer is required.", nameof(buyerId));
        }

        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller is required.", nameof(sellerId));
        }

        ArgumentNullException.ThrowIfNull(deliveryAddress);

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            BuyerId = buyerId,
            SellerId = sellerId,
            Status = OrderStatus.Created,
            DeliveryAddress = deliveryAddress,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var (product, quantity) in lines)
        {
            order.AddItem(product, quantity);
        }

        if (order._items.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one item.");
        }

        return order;
    }

    public void ChangeDeliveryAddress(DeliveryAddress deliveryAddress)
    {
        ArgumentNullException.ThrowIfNull(deliveryAddress);

        if (Status is not (OrderStatus.Created or OrderStatus.Paid))
        {
            throw new InvalidOperationException($"The delivery address of an order in status {Status} cannot be changed.");
        }

        DeliveryAddress = deliveryAddress;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Paid, "shipped");

        Status = OrderStatus.Shipped;
        ShippedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deliver()
    {
        EnsureStatus(OrderStatus.Shipped, "delivered");

        Status = OrderStatus.Delivered;
        DeliveredAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        EnsureStatus(OrderStatus.Created, "canceled");
        EnsurePaymentLoaded();

        if (Payment is { Status: PaymentStatus.Pending })
        {
            throw new InvalidOperationException("The pending payment of the order must be canceled first.");
        }

        foreach (var item in _items)
        {
            if (item.Product is null)
            {
                throw new InvalidOperationException("Order items must be loaded with their products to cancel an order.");
            }

            item.Product.IncreaseStock(item.Quantity);
        }

        Status = OrderStatus.Canceled;
        CanceledAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void AttachPayment(Payment payment)
    {
        EnsureStatus(OrderStatus.Created, "paid");
        EnsurePaymentLoaded();

        if (Payment is { Status: PaymentStatus.Pending } current && current != payment)
        {
            throw new InvalidOperationException("The order already has a pending payment.");
        }

        Payment = payment;
        PaymentId = payment.Id;
    }

    internal void MarkPaid(DateTimeOffset paidAt)
    {
        EnsureStatus(OrderStatus.Created, "paid");

        Status = OrderStatus.Paid;
        PaidAt = UpdatedAt = paidAt;
    }

    private void EnsurePaymentLoaded()
    {
        if (PaymentId is not null && Payment is null)
        {
            throw new InvalidOperationException("The order must be loaded with its payment.");
        }
    }

    private void AddItem(Product product, int quantity)
    {
        if (product.SellerId != SellerId)
        {
            throw new InvalidOperationException($"Product {product.Name} belongs to another seller.");
        }

        product.EnsureAvailable(quantity);
        product.DecreaseStock(quantity);

        _items.Add(new OrderItem(Id, product, quantity));
        TotalAmount += decimal.Round(product.Price * quantity, 2);
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"An order in status {Status} cannot be {action}.");
        }
    }
}
