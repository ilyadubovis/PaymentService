using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.Contracts;
using PaymentService.Application.Abstractions;
using PaymentService.Application.Payments.Commands;
using PaymentService.Application.Payments.Queries;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePaymentCommand(
            request.OrderId,
            request.Amount,
            request.Currency,
            request.CustomerEmail,
            request.PaymentMethodId,
            idempotencyKey ?? string.Empty), cancellationToken);

        return result.AlreadyProcessed
            ? Ok(result)
            : CreatedAtAction(nameof(GetById), new { id = result.PaymentId }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Search(
        [FromQuery] string? orderId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new SearchPaymentsQuery(orderId, status, page, pageSize), cancellationToken));
    }

    [HttpPost("{id:guid}/capture")]
    public async Task<ActionResult<CapturePaymentResult>> Capture(
        Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required." });
        return Ok(await sender.Send(new CapturePaymentCommand(id, idempotencyKey), cancellationToken));
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<ActionResult<RefundPaymentResult>> Refund(
        Guid id, RefundPaymentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required." });
        return Ok(await sender.Send(new RefundPaymentCommand(id, request.Amount, idempotencyKey), cancellationToken));
    }
}
