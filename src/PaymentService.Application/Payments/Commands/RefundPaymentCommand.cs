using MediatR;

namespace PaymentService.Application.Payments.Commands;

public sealed record RefundPaymentCommand(Guid PaymentId, decimal Amount, string IdempotencyKey) : IRequest<RefundPaymentResult>;
public sealed record RefundPaymentResult(Guid PaymentId, string Status, decimal RefundedAmount);
