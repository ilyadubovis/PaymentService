using MediatR;

namespace PaymentService.Application.Payments.Queries;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto?>;
