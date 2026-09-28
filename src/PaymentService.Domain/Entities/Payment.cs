using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Domain.Entities;

public sealed class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public string OrderId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AuthorizedAtUtc { get; private set; }
    public DateTime? CapturedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public byte[] RowVersion { get; private set; } = [];

    public static Payment Create(
        string orderId,
        decimal amount,
        string currency,
        string customerEmail,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new DomainException("OrderId is required.");
        if (amount <= 0) throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3) throw new DomainException("Currency must be a 3-letter ISO code.");
        if (string.IsNullOrWhiteSpace(customerEmail)) throw new DomainException("CustomerEmail is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new DomainException("IdempotencyKey is required.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId.Trim(),
            Amount = decimal.Round(amount, 2, MidpointRounding.ToEven),
            Currency = currency.Trim().ToUpperInvariant(),
            CustomerEmail = customerEmail.Trim(),
            Status = PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey.Trim()
        };
    }

    public void MarkRequiresAction(string providerPaymentId)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded)
            throw new DomainException($"Payment cannot require action from status {Status}.");
        ProviderPaymentId = providerPaymentId;
        Status = PaymentStatus.RequiresAction;
    }

    public void MarkAuthorized(string providerPaymentId)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.RequiresAction)) throw new DomainException($"Payment cannot be authorized from status {Status}.");
        ProviderPaymentId = providerPaymentId;
        Status = PaymentStatus.Authorized;
        AuthorizedAtUtc = DateTime.UtcNow;
        FailureCode = null;
        FailureMessage = null;
    }

    public void MarkFailed(string code, string message)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded) throw new DomainException($"Payment cannot be failed from status {Status}.");
        Status = PaymentStatus.Failed;
        FailureCode = code;
        FailureMessage = message;
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized) throw new DomainException($"Only authorized payments can be captured. Current status: {Status}.");
        Status = PaymentStatus.Captured;
        CapturedAtUtc = DateTime.UtcNow;
    }

    public void Refund(decimal amount)
    {
        if (Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
            throw new DomainException($"Only captured payments can be refunded. Current status: {Status}.");
        if (amount <= 0) throw new DomainException("Refund amount must be greater than zero.");
        if (RefundedAmount + amount > Amount) throw new DomainException("Refund amount exceeds the remaining captured amount.");

        RefundedAmount += decimal.Round(amount, 2, MidpointRounding.ToEven);
        RefundedAtUtc = DateTime.UtcNow;
        Status = RefundedAmount == Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
    }
}
