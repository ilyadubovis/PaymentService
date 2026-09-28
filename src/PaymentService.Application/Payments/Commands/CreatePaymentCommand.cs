using MediatR;

namespace PaymentService.Application.Payments.Commands;

public sealed record CreatePaymentCommand(
    string OrderId,
    decimal Amount,
    string Currency,
    string CustomerEmail,
    string PaymentMethodId,
    string IdempotencyKey) : IRequest<CreatePaymentResult>;

public sealed record CreatePaymentResult(
    Guid PaymentId,
    string Status,
    string? ProviderPaymentId,
    string? ClientSecret,
    bool RequiresAction,
    bool AlreadyProcessed);
