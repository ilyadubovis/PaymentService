using PaymentService.Application.Payments.Queries;

namespace PaymentService.Application.Abstractions;

public interface IPaymentReadRepository
{
    Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<PaymentDto>> SearchAsync(string? orderId, string? status, int page, int pageSize, CancellationToken cancellationToken);
}
