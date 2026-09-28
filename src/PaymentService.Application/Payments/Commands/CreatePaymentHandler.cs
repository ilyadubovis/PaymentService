using MediatR;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Payments.Commands;

public sealed class CreatePaymentHandler(
    IPaymentRepository repository,
    IPaymentProcessor processor,
    IPaymentCache cache) : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
{
    public async Task<CreatePaymentResult> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return new CreatePaymentResult(existing.Id, existing.Status.ToString(), existing.ProviderPaymentId, null, existing.Status == PaymentService.Domain.Enums.PaymentStatus.RequiresAction, true);

        var payment = Payment.Create(request.OrderId, request.Amount, request.Currency, request.CustomerEmail, request.IdempotencyKey);
        await repository.AddAsync(payment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var authorization = await processor.AuthorizeAsync(
            payment.Id, payment.Amount, payment.Currency, request.PaymentMethodId,
            payment.CustomerEmail, request.IdempotencyKey, cancellationToken);

        if (authorization.RequiresAction)
            payment.MarkRequiresAction(authorization.ProviderPaymentId!);
        else if (authorization.Succeeded)
            payment.MarkAuthorized(authorization.ProviderPaymentId!);
        else
            payment.MarkFailed(authorization.ErrorCode ?? "processor_error", authorization.ErrorMessage ?? "Payment processor rejected the payment.");

        await repository.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(payment.Id, cancellationToken);

        return new CreatePaymentResult(
            payment.Id, payment.Status.ToString(), payment.ProviderPaymentId,
            authorization.ClientSecret, authorization.RequiresAction, false);
    }
}
