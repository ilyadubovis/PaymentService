# PaymentService - .NET 8 + CQRS + MediatR + SQL Server + Stripe.NET

A production-oriented payment Web API starter using Clean Architecture boundaries, CQRS, MediatR, EF Core, SQL Server, Stripe PaymentIntents, idempotency, webhook reconciliation, and a provider abstraction.

## Architecture

```text
                         HTTP / Stripe.js
                               |
                               v
+------------------+     +-------------------+
| PaymentService   |     | Stripe Webhook    |
| Api              |     | Endpoint          |
+--------+---------+     +---------+---------+
         |                         |
         | MediatR                 | MediatR
         v                         v
+------------------------------------------------+
| PaymentService.Application                     |
| Commands / Queries / Handlers / Abstractions   |
+----------------------+-------------------------+
                       |
                       v
+------------------------------------------------+
| PaymentService.Domain                           |
| Payment aggregate / state transitions          |
+----------------------+-------------------------+
                       |
                       v
+------------------------------------------------+
| PaymentService.Infrastructure                  |
| EF Core | SQL Server | Stripe.NET adapter      |
+----------------------+-------------------------+
                       |
                 +-----+------+
                 |            |
                 v            v
             SQL Server    Stripe API
```

## Projects

```text
PaymentService.sln
├── src
│   ├── PaymentService.Api
│   ├── PaymentService.Application
│   ├── PaymentService.Domain
│   └── PaymentService.Infrastructure
├── tests
│   └── PaymentService.Tests
├── docs
│   ├── architecture.md
│   ├── stripe-integration.md
│   ├── stripe-client-example.html
│   ├── sample.http
│   └── schema.sql
├── Dockerfile
└── docker-compose.yml
```

## CQRS messages

| HTTP | MediatR message | Purpose |
|---|---|---|
| POST `/api/payments` | `CreatePaymentCommand` | Create and confirm Stripe PaymentIntent with manual capture |
| GET `/api/payments/{id}` | `GetPaymentByIdQuery` | Read one payment |
| GET `/api/payments` | `SearchPaymentsQuery` | Search/paginate payments |
| POST `/api/payments/{id}/capture` | `CapturePaymentCommand` | Capture an authorized PaymentIntent |
| POST `/api/payments/{id}/refund` | `RefundPaymentCommand` | Full or partial Stripe refund |
| POST `/api/stripe/webhook` | `ProcessStripePaymentIntentEventCommand` | Reconcile Stripe events into local state |

## Stripe payment flow

The API uses a Stripe PaymentIntent with `capture_method=manual` so authorization and capture are separate application commands.

1. The browser uses Stripe.js / Payment Element to collect card details and creates a Stripe `PaymentMethod`.
2. The browser sends only the `PaymentMethodId` (`pm_...`) to this API.
3. `CreatePaymentHandler` persists the local payment and calls `IPaymentProcessor`.
4. `StripePaymentProcessor` creates and confirms a PaymentIntent using Stripe.NET.
5. If Stripe returns `requires_capture`, the local payment becomes `Authorized`.
6. If Stripe returns `requires_action`, the local payment becomes `RequiresAction` and the API returns the PaymentIntent client secret. The browser completes 3DS/customer authentication with Stripe.js.
7. Stripe webhook `payment_intent.amount_capturable_updated` reconciles the local payment to `Authorized`.
8. The application calls `/capture`, which invokes Stripe's PaymentIntent capture API.
9. The application calls `/refund` for full or partial refunds.
10. Webhooks reconcile success and failure events.

PaymentIntents are designed for payment flows that may require additional authentication such as 3DS. Stripe also recommends using payment events such as `payment_intent.succeeded` as payment-success signals. See the official Stripe resources linked in `docs/stripe-integration.md`.

## Configuration

Do not commit real Stripe credentials. Use environment variables, .NET user secrets, Azure Key Vault, AWS Secrets Manager, or another secret store.

Example environment variables:

```text
Stripe__SecretKey=sk_test_...
Stripe__Webhook__SigningSecret=whsec_...
ConnectionStrings__Payments=Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123!;TrustServerCertificate=True
```

The checked-in `appsettings.json` contains placeholders only.

## Run

Install the .NET 8 SDK and Docker Desktop.

```bash
docker compose up -d
dotnet restore
dotnet build
dotnet test
dotnet run --project src/PaymentService.Api
```

The sample currently uses `EnsureCreated()` for convenience. For production, use EF Core migrations and a controlled deployment process.

## Stripe CLI webhook testing

```bash
stripe login
stripe listen --forward-to https://localhost:7210/api/stripe/webhook
```

Copy the `whsec_...` signing secret printed by Stripe CLI into `Stripe__Webhook__SigningSecret`.

You can trigger a test event with the Stripe CLI, for example:

```bash
stripe trigger payment_intent.succeeded
```

## Important payment-safety rules

- Never accept raw PAN or CVV in this API.
- Use Stripe.js, Elements, or Payment Element to tokenize payment details.
- Store Stripe PaymentIntent IDs and PaymentMethod IDs only when needed; never store raw card data.
- Use a unique idempotency key for each logical operation.
- Never blindly retry a payment after an HTTP timeout. A timeout is an ambiguous outcome: Stripe may have processed the request. Reconcile using the PaymentIntent ID, Stripe idempotency key, and webhook events.
- Verify Stripe webhook signatures.
- Make webhook processing idempotent. Stripe can retry event delivery.
- For strict once-only event handling at scale, persist Stripe event IDs in a unique database table.
- The example focuses on card/PaymentIntent flows. Additional asynchronous payment methods may require more payment states and fulfillment logic.

## Stripe SDK version

The solution pins `Stripe.net` to stable version `52.4.1` rather than a prerelease. The current NuGet listing shows newer prerelease builds as well as the stable 52.x line; pinning a version helps avoid unexpected SDK changes during builds.

## Redis caching

Redis is used as a read-through cache for the CQRS query side. `GetPaymentByIdQuery` caches individual payment DTOs for 5 minutes. `SearchPaymentsQuery` caches paged search results for 30 seconds. Write commands and Stripe webhook reconciliation invalidate the individual payment cache and advance a Redis search-cache version, making existing search keys logically stale without requiring a Redis key scan.

Run Redis with SQL Server:

```bash
docker compose up -d sqlserver redis
```

Default Redis connection: `localhost:6379`. Change `ConnectionStrings:Redis` for another environment.

For production, consider Redis authentication/TLS, bounded cache sizes, observability, and a managed Redis service. The database remains the source of truth; Redis is never required to recover payment state.
