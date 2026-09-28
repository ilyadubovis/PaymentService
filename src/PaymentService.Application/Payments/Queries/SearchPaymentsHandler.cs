using MediatR;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.Payments.Queries;

public sealed class SearchPaymentsHandler(
    IPaymentReadRepository repository,
    IPaymentCache cache) : IRequestHandler<SearchPaymentsQuery, PagedResult<PaymentDto>>
{
    public async Task<PagedResult<PaymentDto>> Handle(SearchPaymentsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var cached = await cache.GetSearchAsync(request.OrderId, request.Status, page, pageSize, cancellationToken);
        if (cached is not null)
            return cached;

        var result = await repository.SearchAsync(request.OrderId, request.Status, page, pageSize, cancellationToken);
        await cache.SetSearchAsync(request.OrderId, request.Status, page, pageSize, result, cancellationToken);
        return result;
    }
}
