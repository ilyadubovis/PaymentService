using Xunit;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Tests;

public class PaymentDomainTests
{
    [Fact]
    public void Create_should_start_as_pending()
    {
        var payment = Payment.Create("ORDER-1", 100m, "usd", "customer@example.com", "idem-1");
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("USD", payment.Currency);
    }

    [Fact]
    public void Capture_requires_authorization()
    {
        var payment = Payment.Create("ORDER-1", 100m, "USD", "customer@example.com", "idem-1");
        Assert.Throws<DomainException>(() => payment.Capture());
    }

    [Fact]
    public void Full_refund_changes_status_to_refunded()
    {
        var payment = Payment.Create("ORDER-1", 100m, "USD", "customer@example.com", "idem-1");
        payment.MarkAuthorized("provider-1");
        payment.Capture();
        payment.Refund(100m);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(100m, payment.RefundedAmount);
    }
}
