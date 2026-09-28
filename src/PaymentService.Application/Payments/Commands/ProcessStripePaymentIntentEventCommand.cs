using MediatR;

namespace PaymentService.Application.Payments.Commands;

public sealed record ProcessStripePaymentIntentEventCommand(
    string ProviderPaymentId,
    string StripeStatus,
    string? FailureCode,
    string? FailureMessage) : IRequest;
