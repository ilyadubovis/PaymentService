using MediatR;

namespace PaymentService.Application.Payments.Commands;

public sealed record CapturePaymentCommand(Guid PaymentId, string IdempotencyKey) : IRequest<CapturePaymentResult>;
public sealed record CapturePaymentResult(Guid PaymentId, string Status);
