namespace PaymentService.Application.Abstractions;

public sealed record ProcessorAuthorizationResult(
    bool Succeeded,
    bool RequiresAction,
    string? ProviderPaymentId,
    string? ClientSecret,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ProcessorResult(bool Succeeded, string? ErrorCode, string? ErrorMessage);

public interface IPaymentProcessor
{
    Task<ProcessorAuthorizationResult> AuthorizeAsync(
        Guid paymentId, decimal amount, string currency, string paymentMethodId,
        string customerEmail, string idempotencyKey, CancellationToken cancellationToken);

    Task<ProcessorResult> CaptureAsync(string providerPaymentId, decimal amount, string currency, string idempotencyKey, CancellationToken cancellationToken);
    Task<ProcessorResult> RefundAsync(string providerPaymentId, decimal amount, string currency, string idempotencyKey, CancellationToken cancellationToken);
}
