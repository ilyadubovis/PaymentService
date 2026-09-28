using MediatR;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.Payments.Commands;

public sealed class CapturePaymentHandler(
    IPaymentRepository repository,
    IPaymentProcessor processor,
    IPaymentCache cache) : IRequestHandler<CapturePaymentCommand, CapturePaymentResult>
{
    public async Task<CapturePaymentResult> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment {request.PaymentId} was not found.");

        if (payment.ProviderPaymentId is null)
            throw new InvalidOperationException("The payment does not have a provider payment id.");

        var result = await processor.CaptureAsync(payment.ProviderPaymentId, payment.Amount, payment.Currency, request.IdempotencyKey, cancellationToken);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.ErrorMessage ?? "Payment capture failed.");

        payment.Capture();
        await repository.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(payment.Id, cancellationToken);
        return new CapturePaymentResult(payment.Id, payment.Status.ToString());
    }
}
