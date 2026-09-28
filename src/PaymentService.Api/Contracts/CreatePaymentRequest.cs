namespace PaymentService.Api.Contracts;

public sealed record CreatePaymentRequest(
    string OrderId,
    decimal Amount,
    string Currency,
    string CustomerEmail,
    string PaymentMethodId);
