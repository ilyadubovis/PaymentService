using MediatR;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.Payments.Commands;

public sealed class RefundPaymentHandler(
    IPaymentRepository repository,
    IPaymentProcessor processor,
    IPaymentCache cache) : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment {request.PaymentId} was not found.");

        if (payment.ProviderPaymentId is null)
            throw new InvalidOperationException("The payment does not have a provider payment id.");

        var result = await processor.RefundAsync(payment.ProviderPaymentId, request.Amount, payment.Currency, request.IdempotencyKey, cancellationToken);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.ErrorMessage ?? "Payment refund failed.");

        payment.Refund(request.Amount);
        await repository.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(payment.Id, cancellationToken);
        return new RefundPaymentResult(payment.Id, payment.Status.ToString(), payment.RefundedAmount);
    }
}
