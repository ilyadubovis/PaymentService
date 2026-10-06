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
