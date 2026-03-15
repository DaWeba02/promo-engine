using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromoEngine.Application.Abstractions;
using PromoEngine.Infrastructure.Persistence;
using PromoEngine.Infrastructure.Persistence.Repositories;

namespace PromoEngine.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? "Server=localhost,14333;Database=PromoEngine;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";

        services.AddDbContext<PromoEngineDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IQuoteAuditRepository, QuoteAuditRepository>();
        services.AddScoped<IBudgetConsumptionRepository, BudgetConsumptionRepository>();
        services.AddScoped<IPromotionRedemptionRepository, PromotionRedemptionRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<PromoEngineDbContext>());
        return services;
    }
}
