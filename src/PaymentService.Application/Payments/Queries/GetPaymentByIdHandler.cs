using MediatR;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.Payments.Queries;

public sealed class GetPaymentByIdHandler(
    IPaymentReadRepository repository,
    IPaymentCache cache) : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetByIdAsync(request.PaymentId, cancellationToken);
        if (cached is not null)
            return cached;

        var payment = await repository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is not null)
            await cache.SetByIdAsync(payment, cancellationToken);

        return payment;
    }
}
