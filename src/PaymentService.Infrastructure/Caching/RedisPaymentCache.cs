using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentService.Application.Abstractions;
using PaymentService.Application.Payments.Queries;
using StackExchange.Redis;

namespace PaymentService.Infrastructure.Caching;

public sealed class RedisPaymentCache(IConnectionMultiplexer redis) : IPaymentCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string SearchVersionKey = "payment-service:payments:search-version";
    private const string PaymentPrefix = "payment-service:payment:";
    private static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SearchTtl = TimeSpan.FromSeconds(30);

    public async Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await Database.StringGetAsync(PaymentPrefix + paymentId);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PaymentDto>(value!, JsonOptions);
    }

    public async Task SetByIdAsync(PaymentDto payment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(payment, JsonOptions);
        await Database.StringSetAsync(PaymentPrefix + payment.Id, json, PaymentTtl);
    }

    public async Task<PagedResult<PaymentDto>?> GetSearchAsync(string? orderId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var version = await GetSearchVersionAsync();
        var key = BuildSearchKey(version, orderId, status, page, pageSize);
        var value = await Database.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PagedResult<PaymentDto>>(value!, JsonOptions);
    }

    public async Task SetSearchAsync(string? orderId, string? status, int page, int pageSize, PagedResult<PaymentDto> result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var version = await GetSearchVersionAsync();
        var key = BuildSearchKey(version, orderId, status, page, pageSize);
        var json = JsonSerializer.Serialize(result, JsonOptions);
        await Database.StringSetAsync(key, json, SearchTtl);
    }

    public async Task InvalidateAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Database.KeyDeleteAsync(PaymentPrefix + paymentId);
        await Database.StringIncrementAsync(SearchVersionKey);
    }

    private IDatabase Database => redis.GetDatabase();

    private async Task<long> GetSearchVersionAsync()
    {
        var value = await Database.StringGetAsync(SearchVersionKey);
        if (value.HasValue && long.TryParse(value, out var version))
            return version;

        await Database.StringSetAsync(SearchVersionKey, 1, keepTtl: true);
        return 1;
    }

    private static string BuildSearchKey(long version, string? orderId, string? status, int page, int pageSize)
    {
        var input = $"{version}|{orderId ?? string.Empty}|{status ?? string.Empty}|{page}|{pageSize}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        return $"payment-service:payments:search:{hash}";
    }
}
