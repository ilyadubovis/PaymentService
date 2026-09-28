using PaymentService.Domain.Enums;

namespace PaymentService.Application.Payments.Queries;

public sealed record PaymentDto(
    Guid Id,
    string OrderId,
    decimal Amount,
    string Currency,
    string CustomerEmail,
    PaymentStatus Status,
    string? ProviderPaymentId,
    string? FailureCode,
    string? FailureMessage,
    DateTime CreatedAtUtc,
    DateTime? AuthorizedAtUtc,
    DateTime? CapturedAtUtc,
    DateTime? RefundedAtUtc,
    decimal RefundedAmount,
    string IdempotencyKey);
