using MediatR;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.Payments.Queries;

public sealed record SearchPaymentsQuery(string? OrderId, string? Status, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<PaymentDto>>;
