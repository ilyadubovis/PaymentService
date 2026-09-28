namespace PaymentService.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    RequiresAction = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
