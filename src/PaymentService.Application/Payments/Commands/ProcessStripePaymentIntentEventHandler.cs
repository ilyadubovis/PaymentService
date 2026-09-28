using MediatR;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Payments.Commands;

public sealed class ProcessStripePaymentIntentEventHandler(
    IPaymentRepository repository,
    IPaymentCache cache) : IRequestHandler<ProcessStripePaymentIntentEventCommand>
{
    public async Task Handle(ProcessStripePaymentIntentEventCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByProviderPaymentIdAsync(request.ProviderPaymentId, cancellationToken);
        if (payment is null)
            return;

        switch (request.StripeStatus)
        {
            case "requires_capture":
                if (payment.Status is PaymentStatus.Pending or PaymentStatus.RequiresAction)
                    payment.MarkAuthorized(request.ProviderPaymentId);
                break;

            case "succeeded":
                if (payment.Status == PaymentStatus.Authorized)
                    payment.Capture();
                break;

            case "requires_payment_method":
            case "canceled":
                if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.Refunded))
                    payment.MarkFailed(request.FailureCode ?? "stripe_payment_failed", request.FailureMessage ?? $"Stripe PaymentIntent status: {request.StripeStatus}");
                break;
        }

        await repository.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(payment.Id, cancellationToken);
    }
}
