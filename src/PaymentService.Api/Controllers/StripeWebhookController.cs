using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.Application.Payments.Commands;
using PaymentService.Infrastructure.Payments;
using Stripe;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public sealed class StripeWebhookController(
    ISender sender,
    IOptions<StripeWebhookOptions> options) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(options.Value.SigningSecret) || string.IsNullOrWhiteSpace(signature))
            return BadRequest();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, options.Value.SigningSecret);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        if (stripeEvent.Data.Object is PaymentIntent intent)
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.PaymentIntentAmountCapturableUpdated:
                    await sender.Send(new ProcessStripePaymentIntentEventCommand(
                        intent.Id, intent.Status, null, null), cancellationToken);
                    break;
                case EventTypes.PaymentIntentSucceeded:
                    await sender.Send(new ProcessStripePaymentIntentEventCommand(
                        intent.Id, intent.Status, null, null), cancellationToken);
                    break;
                case EventTypes.PaymentIntentPaymentFailed:
                    await sender.Send(new ProcessStripePaymentIntentEventCommand(
                        intent.Id, intent.Status, intent.LastPaymentError?.Code, intent.LastPaymentError?.Message), cancellationToken);
                    break;
                case EventTypes.PaymentIntentCanceled:
                    await sender.Send(new ProcessStripePaymentIntentEventCommand(
                        intent.Id, intent.Status, intent.CancellationReason, "PaymentIntent was canceled."), cancellationToken);
                    break;
            }
        }

        return Ok();
    }
}

public sealed class StripeWebhookOptions
{
    public string SigningSecret { get; set; } = string.Empty;
}
