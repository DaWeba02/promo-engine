using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Services;
using PromoEngine.Domain.Abstractions;
using PromoEngine.Domain.Services;
using PromoEngine.Infrastructure.DependencyInjection;
using PromoEngine.Infrastructure.Persistence;
using PromoEngine.Api.Endpoints;
using PromoEngine.Api.Extensions;
using PromoEngine.Api.Validators;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<EnumSchemaDescriptionFilter>();
    options.OperationFilter<RequestEnumOperationFilter>();
});
builder.Services.AddValidatorsFromAssemblyContaining<QuoteRequestValidator>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPricingEngine, PricingEngine>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddDbContextCheck<PromoEngineDbContext>("sqlserver");

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PromoEngineDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGet("/ping", () => Results.Ok(new { status = "pong", utcNow = DateTimeOffset.UtcNow }));
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = registration => registration.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions());
app.MapQuoteEndpoints();
app.MapPromotionEndpoints();

app.Run();

public partial class Program;
