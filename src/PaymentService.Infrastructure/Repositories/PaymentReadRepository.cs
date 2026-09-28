using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Abstractions;
using PaymentService.Application.Payments.Queries;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public sealed class PaymentReadRepository(PaymentDbContext db) : IPaymentReadRepository
{
    public Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Payments.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PaymentDto(
                x.Id, x.OrderId, x.Amount, x.Currency, x.CustomerEmail, x.Status,
                x.ProviderPaymentId, x.FailureCode, x.FailureMessage,
                x.CreatedAtUtc, x.AuthorizedAtUtc, x.CapturedAtUtc, x.RefundedAtUtc,
                x.RefundedAmount, x.IdempotencyKey))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<PaymentDto>> SearchAsync(
        string? orderId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderId))
            query = query.Where(x => x.OrderId == orderId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentDto(
                x.Id, x.OrderId, x.Amount, x.Currency, x.CustomerEmail, x.Status,
                x.ProviderPaymentId, x.FailureCode, x.FailureMessage,
                x.CreatedAtUtc, x.AuthorizedAtUtc, x.CapturedAtUtc, x.RefundedAtUtc,
                x.RefundedAmount, x.IdempotencyKey))
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentDto>(items, page, pageSize, total);
    }
}
