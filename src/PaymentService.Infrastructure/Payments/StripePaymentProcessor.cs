using Microsoft.Extensions.Options;
using PaymentService.Application.Abstractions;
using Stripe;

namespace PaymentService.Infrastructure.Payments;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Stripe adapter. The application layer knows only IPaymentProcessor.
/// PaymentMethodId is a Stripe token created by Stripe.js/Elements on the client.
/// Never send raw card numbers or CVV through this API.
/// </summary>
public sealed class StripePaymentProcessor(IOptions<StripeOptions> options) : IPaymentProcessor
{
    private readonly PaymentIntentService _paymentIntents = new();
    private readonly RefundService _refunds = new();

    public async Task<ProcessorAuthorizationResult> AuthorizeAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string paymentMethodId,
        string customerEmail,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
            throw new InvalidOperationException("Stripe:SecretKey is not configured.");

        StripeConfiguration.ApiKey = options.Value.SecretKey;

        var createOptions = new PaymentIntentCreateOptions
        {
            Amount = checked((long)Math.Round(amount * 100m, 0, MidpointRounding.ToEven)),
            Currency = currency.ToLowerInvariant(),
            PaymentMethod = paymentMethodId,
            Confirm = true,
            CaptureMethod = "manual",
            ReceiptEmail = customerEmail,
            Description = $"Payment {paymentId}",
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = paymentId.ToString()
            }
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        PaymentIntent intent;

        try
        {
            intent = await _paymentIntents.CreateAsync(createOptions, requestOptions, cancellationToken);
        }
        catch (StripeException ex)
        {
            return new ProcessorAuthorizationResult(
                false,
                false,
                null,
                null,
                ex.StripeError?.Code,
                ex.StripeError?.Message ?? ex.Message);
        }

        return intent.Status switch
        {
            "requires_action" or "requires_confirmation" => new ProcessorAuthorizationResult(
                false, true, intent.Id, intent.ClientSecret, null, "Customer authentication is required."),
            "requires_capture" => new ProcessorAuthorizationResult(
                true, false, intent.Id, intent.ClientSecret, null, null),
            "succeeded" => new ProcessorAuthorizationResult(
                true, false, intent.Id, intent.ClientSecret, null, null),
            _ => new ProcessorAuthorizationResult(
                false, false, intent.Id, intent.ClientSecret, intent.LastPaymentError?.Code,
                intent.LastPaymentError?.Message ?? $"Unexpected Stripe PaymentIntent status: {intent.Status}")
        };
    }

    public async Task<ProcessorResult> CaptureAsync(
        string providerPaymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        try
        {
            var result = await _paymentIntents.CaptureAsync(
                providerPaymentId,
                new PaymentIntentCaptureOptions
                {
                    AmountToCapture = checked((long)Math.Round(amount * 100m, 0, MidpointRounding.ToEven))
                },
                new RequestOptions { IdempotencyKey = idempotencyKey },
                cancellationToken);

            return result.Status is "succeeded" or "requires_capture"
                ? new ProcessorResult(true, null, null)
                : new ProcessorResult(false, result.LastPaymentError?.Code, result.LastPaymentError?.Message ?? $"Stripe status: {result.Status}");
        }
        catch (StripeException ex)
        {
            return new ProcessorResult(false, ex.StripeError?.Code, ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<ProcessorResult> RefundAsync(
        string providerPaymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        try
        {
            var refund = await _refunds.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = providerPaymentId,
                    Amount = checked((long)Math.Round(amount * 100m, 0, MidpointRounding.ToEven))
                },
                new RequestOptions { IdempotencyKey = idempotencyKey },
                cancellationToken);

            return refund.Status == "succeeded"
                ? new ProcessorResult(true, null, null)
                : new ProcessorResult(false, refund.FailureReason, $"Stripe refund status: {refund.Status}");
        }
        catch (StripeException ex)
        {
            return new ProcessorResult(false, ex.StripeError?.Code, ex.StripeError?.Message ?? ex.Message);
        }
    }
}
