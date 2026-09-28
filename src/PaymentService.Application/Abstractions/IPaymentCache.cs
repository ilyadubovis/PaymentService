using PaymentService.Application.Payments.Queries;

namespace PaymentService.Application.Abstractions;

public interface IPaymentCache
{
    Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task SetByIdAsync(PaymentDto payment, CancellationToken cancellationToken);
    Task<PagedResult<PaymentDto>?> GetSearchAsync(string? orderId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task SetSearchAsync(string? orderId, string? status, int page, int pageSize, PagedResult<PaymentDto> result, CancellationToken cancellationToken);
    Task InvalidateAsync(Guid paymentId, CancellationToken cancellationToken);
}
