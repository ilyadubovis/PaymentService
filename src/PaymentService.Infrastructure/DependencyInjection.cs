using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Abstractions;
using PaymentService.Infrastructure.Caching;
using PaymentService.Infrastructure.Payments;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Payments")));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:8080"));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentReadRepository, PaymentReadRepository>();
        services.AddSingleton<IPaymentCache, RedisPaymentCache>();
        services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();

        return services;
    }
}
